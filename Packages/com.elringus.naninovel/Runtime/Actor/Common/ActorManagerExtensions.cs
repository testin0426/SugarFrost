using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IActorManager"/>.
    /// </summary>
    public static class ActorManagerExtensions
    {
        /// <summary>
        /// Returns a managed actor with the specified ID; throws when not found.
        /// </summary>
        public static IActor GetActorOrErr (this IActorManager manager, string actorId)
        {
            return manager.GetActor(actorId) ?? throw new Error($"Can't find '{actorId}' actor.");
        }

        /// <summary>
        /// Returns a managed actor with the specified ID; throws when not found.
        /// </summary>
        public static TActor GetActorOrErr<TActor, TState, TMeta, TConfig> (
            this IActorManager<TActor, TState, TMeta, TConfig> manager, string actorId)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            return manager.GetActor(actorId) ?? throw new Error($"Can't find '{actorId}' actor.");
        }

        /// <summary>
        /// Returns a managed actor with the specified ID. If the actor doesn't exist, will add it.
        /// </summary>
        public static async Awaitable<IActor> GetOrAddActor (this IActorManager manager, string actorId)
        {
            return manager.ActorExists(actorId) ? manager.GetActorOrErr(actorId) : await manager.AddActor(actorId);
        }

        /// <summary>
        /// Returns a managed actor with the specified ID. If the actor doesn't exist, will add it.
        /// </summary>
        public static async Awaitable<TActor> GetOrAddActor<TActor, TState, TMeta, TConfig> (
            this IActorManager<TActor, TState, TMeta, TConfig> manager, string actorId)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            return manager.ActorExists(actorId) ? manager.GetActorOrErr(actorId) : await manager.AddActor(actorId);
        }

        /// <summary>
        /// Retrieves metadata of the actor with specified ID or default when actor with the ID is not found.
        /// </summary>
        public static TMeta GetActorMetaOrDefault<TActor, TState, TMeta, TConfig> (
            this IActorManager<TActor, TState, TMeta, TConfig> manager, string actorId)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            return manager.Configuration.GetMetadataOrDefault(actorId);
        }

        /// <summary>
        /// Rents a pooled list and collects all the managed actors.
        /// </summary>
        public static IDisposable RentActors (this IActorManager manager, out List<IActor> actors)
        {
            var rent = ListPool<IActor>.Rent(out actors);
            manager.CollectActors(actors);
            return rent;
        }

        /// <summary>
        /// Rents a pooled list and collects all the managed actors.
        /// </summary>
        public static IDisposable RentActors<TActor, TState, TMeta, TConfig> (
            this IActorManager<TActor, TState, TMeta, TConfig> manager, out List<TActor> actors)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            var rent = ListPool<TActor>.Rent(out actors);
            manager.CollectActors(actors);
            return rent;
        }

        /// <summary>
        /// Whether any of the currently managed actors satisfy the specified filter.
        /// when filter is not specified returns whether any actors are managed at all.
        /// </summary>
        public static bool AnyActor (this IActorManager manager, [CanBeNull] Predicate<IActor> filter = null)
        {
            if (filter == null) return manager.FindActor(_ => true) != null;
            return manager.FindActor(filter) != null;
        }

        /// <inheritdoc cref="AnyActor"/>
        public static bool AnyActor<TActor, TState, TMeta, TConfig> (
            this IActorManager<TActor, TState, TMeta, TConfig> manager, [CanBeNull] Predicate<TActor> filter = null)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            if (filter == null) return manager.FindActor(_ => true) != null;
            return manager.FindActor(filter) != null;
        }
    }
}
