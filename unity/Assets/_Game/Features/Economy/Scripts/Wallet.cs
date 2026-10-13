using System;
using System.Collections.Generic;

namespace HoldTheHill.Features.Economy
{
    /// <summary>
    /// What the player holds during a run: a main resource (Food) and any others, each named by a
    /// <see cref="ResourceData"/>. Plain C#, so the rules can be tested without a scene;
    /// <see cref="WalletBehaviour"/> puts one in a scene.
    /// </summary>
    /// <remarks>
    /// A null resource means the main one. A resource never earned or spent starts at its
    /// <see cref="ResourceData.StartingAmount"/>. Amounts never go below zero: spending more than
    /// is held fails and changes nothing.
    /// </remarks>
    public sealed class Wallet
    {
        private readonly Dictionary<ResourceData, int> _others = new Dictionary<ResourceData, int>();
        private ResourceData _main;
        private int _mainAmount;

        /// <summary>Raised when the amount of any resource changes, with the resource (the main one, possibly null) and its new amount.</summary>
        public event Action<ResourceData, int> Changed;

        /// <summary>A wallet whose main resource starts at <paramref name="startingMain"/>.</summary>
        public Wallet(ResourceData main, int startingMain)
        {
            _main = main;
            _mainAmount = Math.Max(0, startingMain);
        }

        /// <summary>
        /// The main resource, or null when there is none. Changing it keeps the main amount: the
        /// scene decides what the main resource is, the wallet only counts it.
        /// </summary>
        public ResourceData Main
        {
            get => _main;
            set => _main = value;
        }

        /// <summary>How much of the main resource is held.</summary>
        public int MainAmount => _mainAmount;

        private bool IsMain(ResourceData resource) => resource == null || resource == _main;

        /// <summary>How much of a resource is held.</summary>
        public int Get(ResourceData resource)
        {
            if (IsMain(resource))
            {
                return _mainAmount;
            }

            return _others.TryGetValue(resource, out int amount) ? amount : resource.StartingAmount;
        }

        /// <summary>Adds to a resource. Nothing happens for zero or less.</summary>
        public void Earn(ResourceData resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Set(resource, Get(resource) + amount);
        }

        /// <summary>True if at least <paramref name="amount"/> of the resource is held.</summary>
        public bool CanAfford(ResourceData resource, int amount)
        {
            return Get(resource) >= amount;
        }

        /// <summary>Takes from a resource if enough is held. Zero or less always succeeds and changes nothing.</summary>
        public bool TrySpend(ResourceData resource, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (!CanAfford(resource, amount))
            {
                return false;
            }

            Set(resource, Get(resource) - amount);
            return true;
        }

        /// <summary>Puts a resource at an exact amount, never below zero. For run start, restarts and cheats.</summary>
        public void Set(ResourceData resource, int amount)
        {
            amount = Math.Max(0, amount);
            if (IsMain(resource))
            {
                _mainAmount = amount;
                Changed?.Invoke(_main, amount);
                return;
            }

            _others[resource] = amount;
            Changed?.Invoke(resource, amount);
        }
    }
}
