//  Project  : ACTORS
//  Contacts : Pixeye - ask@pixeye.games

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
 


namespace Pixeye.Framework
{
	delegate void CallBackDelete(int i);

	class GroupCoreComparer : IEqualityComparer<GroupCore>
	{
		public static GroupCoreComparer Default = new GroupCoreComparer();

		public bool Equals(GroupCore x, GroupCore y)
		{
			return y.id == x.id;
		}

		public int GetHashCode(GroupCore obj)
		{
			return obj.composition.hash;
		}
	}


	[Il2CppSetOption(Option.NullChecks | Option.ArrayBoundsChecks | Option.DivideByZeroChecks, false)]
	public abstract class GroupCore : IEnumerable, IEquatable<GroupCore>, IDisposable
	{
		internal delegate void DelRemove(int id);

		static int idCounter;

		public ent[] entities = new ent[Entity.settings.SizeEntities];
		public int length;


		public EntityAction onAdd;
		public EntityAction onRemove;

		internal EntityAction actionInsert;
		internal DelRemove actionTryRemove;
		internal DelRemove actionRemove;

		protected internal Composition composition;
		internal int id;

		int position;

		public ref ent this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref entities[index];
		}

		public void Release(int index)
		{
			if (length == 0) return;
			entities[index].Release();
		}

		internal GroupCore Start(Composition composition)
		{
			this.composition = composition;
			composition.SetupExcludeTypes(this);
			HelperTags.Add(this);
			Initialize();
			return this;
		}

		internal void AddCallbacks()
		{
			if ((object) onAdd != null)
			{
				actionInsert = Insert;
			}
			else
				actionInsert = InsertNoCallback;

			if ((object) onRemove != null)
			{
				actionTryRemove = TryRemove;
				actionRemove    = Remove;
			}
			else
			{
				actionTryRemove = TryRemoveNoCallback;
				actionRemove    = RemoveNoCallBack;
			}
		}

		//===============================//
		// Insert
		//===============================//
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void Insert(in ent entity)
		{
			var left  = 0;
			var index = 0;
			var right = length++;

			if (length >= entities.Length)
				Array.Resize(ref entities, length << 1);


			while (right > left)
			{
				var midIndex = (right + left) / 2;

				if (entities[midIndex].id == entity.id)
				{
					index = midIndex;
					break;
				}

				if (entities[midIndex].id < entity.id)
					left = midIndex + 1;
				else
					right = midIndex;

				index = left;
			}

			Array.Copy(entities, index, entities, index + 1, length - index);
			entities[index] = entity;
			onAdd(entity);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void InsertNoCallback(in ent entity)
		{
			var left  = 0;
			var index = 0;
			var right = length++;

			if (length >= entities.Length)
				Array.Resize(ref entities, length << 1);


			while (right > left)
			{
				var midIndex = (right + left) / 2;

				if (entities[midIndex].id == entity.id)
				{
					index = midIndex;
					break;
				}

				if (entities[midIndex].id < entity.id)
					left = midIndex + 1;
				else
					right = midIndex;

				index = left;
			}

			Array.Copy(entities, index, entities, index + 1, length - index);
			entities[index] = entity;
		}

		//===============================//
		// Try Remove
		//===============================//

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void TryRemove(int entityID)
		{
			if (length == 0) return;
			var i = HelperArray.BinarySearch(ref entities, entityID, 0, length);
			if (i == -1) return;
			onRemove(entities[i]);
			Array.Copy(entities, i + 1, entities, i, length-- - i);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void TryRemoveNoCallback(int entityID)
		{
			if (length == 0) return;
			var i = HelperArray.BinarySearch(ref entities, entityID, 0, length);
			if (i == -1) return;
			Array.Copy(entities, i + 1, entities, i, length-- - i);
		}

		//===============================//
		// Remove
		//===============================//
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Remove(int i)
		{
			onRemove(entities[i]);
			Array.Copy(entities, i + 1, entities, i, length-- - i);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void RemoveNoCallBack(int i)
		{
			Array.Copy(entities, i + 1, entities, i, length-- - i);
		}

		public GroupCore()
		{
			id = idCounter++;
		}

		public abstract void Initialize();

		public void Dispose()
		{
			onAdd    = null;
			onRemove = null;
			length   = 0;
		}

		#region EQUALS

		public bool Equals(GroupCore other)
		{
			return id == other.id;
		}

		public bool Equals(int other)
		{
			return id == other;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((GroupCore) obj);
		}

		public override int GetHashCode()
		{
			return id;
		}

		#endregion

		#region ENUMERATOR

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this, length);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public struct Enumerator : IEnumerator<ent>
		{
			GroupCore g;
			int position;
			int length;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Enumerator(GroupCore g, int length)
			{
				position    = -1;
				this.g      = g;
				this.length = length;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				return ++position < length;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset()
			{
				position = -1;
			}

			object IEnumerator.Current => Current;

			public ent Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get { return g.entities[position]; }
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
			}
		}

		#endregion
	}

	public class Group<T> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);

			var len = 1;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0] = Storage<T>.generation;
			composition.ids[0]         = Storage<T>.componentMask;

			composition.includeComponents[Storage<T>.componentID] = true;
		}
	}

	public class Group<T, Y> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);

			var len = 2;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);

			var len = 3;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);

			var len = 4;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I, O> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			var len = 5;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0] = Storage<T>.generation;
			composition.ids[0]         = Storage<T>.componentMask;

			composition.generations[1] = Storage<Y>.generation;
			composition.ids[1]         = Storage<Y>.componentMask;

			composition.generations[2] = Storage<U>.generation;
			composition.ids[2]         = Storage<U>.componentMask;

			composition.generations[3] = Storage<I>.generation;
			composition.ids[3]         = Storage<I>.componentMask;

			composition.generations[4] = Storage<O>.generation;
			composition.ids[4]         = Storage<O>.componentMask;
		}
	}

	public sealed class Group<T, Y, U, I, O, P> : GroupCore

	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			Storage<P>.Instance.Add(this);

			var len = 6;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;

			composition.generations[4]                            = Storage<O>.generation;
			composition.ids[4]                                    = Storage<O>.componentMask;
			composition.includeComponents[Storage<O>.componentID] = true;

			composition.generations[5]                            = Storage<P>.generation;
			composition.ids[5]                                    = Storage<P>.componentMask;
			composition.includeComponents[Storage<P>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I, O, P, A> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			Storage<P>.Instance.Add(this);
			Storage<A>.Instance.Add(this);

			var len = 7;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;

			composition.generations[4]                            = Storage<O>.generation;
			composition.ids[4]                                    = Storage<O>.componentMask;
			composition.includeComponents[Storage<O>.componentID] = true;

			composition.generations[5]                            = Storage<P>.generation;
			composition.ids[5]                                    = Storage<P>.componentMask;
			composition.includeComponents[Storage<P>.componentID] = true;

			composition.generations[6]                            = Storage<A>.generation;
			composition.ids[6]                                    = Storage<A>.componentMask;
			composition.includeComponents[Storage<A>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I, O, P, A, S> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			Storage<P>.Instance.Add(this);
			Storage<A>.Instance.Add(this);
			Storage<S>.Instance.Add(this);

			var len = 8;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;

			composition.generations[4]                            = Storage<O>.generation;
			composition.ids[4]                                    = Storage<O>.componentMask;
			composition.includeComponents[Storage<O>.componentID] = true;

			composition.generations[5]                            = Storage<P>.generation;
			composition.ids[5]                                    = Storage<P>.componentMask;
			composition.includeComponents[Storage<P>.componentID] = true;

			composition.generations[6]                            = Storage<A>.generation;
			composition.ids[6]                                    = Storage<A>.componentMask;
			composition.includeComponents[Storage<A>.componentID] = true;

			composition.generations[7]                            = Storage<S>.generation;
			composition.ids[7]                                    = Storage<S>.componentMask;
			composition.includeComponents[Storage<S>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I, O, P, A, S, D> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			Storage<P>.Instance.Add(this);
			Storage<A>.Instance.Add(this);
			Storage<S>.Instance.Add(this);
			Storage<D>.Instance.Add(this);
			var len = 9;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;

			composition.generations[4]                            = Storage<O>.generation;
			composition.ids[4]                                    = Storage<O>.componentMask;
			composition.includeComponents[Storage<O>.componentID] = true;

			composition.generations[5]                            = Storage<P>.generation;
			composition.ids[5]                                    = Storage<P>.componentMask;
			composition.includeComponents[Storage<P>.componentID] = true;

			composition.generations[6]                            = Storage<A>.generation;
			composition.ids[6]                                    = Storage<A>.componentMask;
			composition.includeComponents[Storage<A>.componentID] = true;

			composition.generations[7]                            = Storage<S>.generation;
			composition.ids[7]                                    = Storage<S>.componentMask;
			composition.includeComponents[Storage<S>.componentID] = true;

			composition.generations[8]                            = Storage<D>.generation;
			composition.ids[8]                                    = Storage<D>.componentMask;
			composition.includeComponents[Storage<D>.componentID] = true;
		}
	}

	public sealed class Group<T, Y, U, I, O, P, A, S, D, F> : GroupCore
	{
		public override void Initialize()
		{
			Storage<T>.Instance.Add(this);
			Storage<Y>.Instance.Add(this);
			Storage<U>.Instance.Add(this);
			Storage<I>.Instance.Add(this);
			Storage<O>.Instance.Add(this);
			Storage<P>.Instance.Add(this);
			Storage<A>.Instance.Add(this);
			Storage<S>.Instance.Add(this);
			Storage<D>.Instance.Add(this);
			Storage<F>.Instance.Add(this);
			var len = 10;

			composition.generations = new int[len];
			composition.ids         = new int[len];

			composition.generations[0]                            = Storage<T>.generation;
			composition.ids[0]                                    = Storage<T>.componentMask;
			composition.includeComponents[Storage<T>.componentID] = true;

			composition.generations[1]                            = Storage<Y>.generation;
			composition.ids[1]                                    = Storage<Y>.componentMask;
			composition.includeComponents[Storage<Y>.componentID] = true;

			composition.generations[2]                            = Storage<U>.generation;
			composition.ids[2]                                    = Storage<U>.componentMask;
			composition.includeComponents[Storage<U>.componentID] = true;

			composition.generations[3]                            = Storage<I>.generation;
			composition.ids[3]                                    = Storage<I>.componentMask;
			composition.includeComponents[Storage<I>.componentID] = true;

			composition.generations[4]                            = Storage<O>.generation;
			composition.ids[4]                                    = Storage<O>.componentMask;
			composition.includeComponents[Storage<O>.componentID] = true;

			composition.generations[5]                            = Storage<P>.generation;
			composition.ids[5]                                    = Storage<P>.componentMask;
			composition.includeComponents[Storage<P>.componentID] = true;

			composition.generations[6]                            = Storage<A>.generation;
			composition.ids[6]                                    = Storage<A>.componentMask;
			composition.includeComponents[Storage<A>.componentID] = true;

			composition.generations[7]                            = Storage<S>.generation;
			composition.ids[7]                                    = Storage<S>.componentMask;
			composition.includeComponents[Storage<S>.componentID] = true;

			composition.generations[8]                            = Storage<D>.generation;
			composition.ids[8]                                    = Storage<D>.componentMask;
			composition.includeComponents[Storage<D>.componentID] = true;

			composition.generations[9]                            = Storage<F>.generation;
			composition.ids[9]                                    = Storage<F>.componentMask;
			composition.includeComponents[Storage<F>.componentID] = true;
		}
	}
}