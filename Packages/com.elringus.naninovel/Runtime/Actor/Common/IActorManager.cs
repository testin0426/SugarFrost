using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IActor"/> actors.
    /// </summary>
    public interface IActorManager : IEngineService
    {
        /// <summary>
        /// Invoked when an actor with the ID is added to the manager.
        /// </summary>
        event Action<string> OnActorAdded;
        /// <summary>
        /// Invoked when an actor with the ID is removed from the manager.
        /// </summary>
        event Action<string> OnActorRemoved;

        /// <summary>
        /// Base configuration of the manager.
        /// </summary>
        ActorManagerConfiguration ActorManagerConfiguration { get; }

        /// <summary>
        /// Checks whether an actor with specified ID is currently managed
        /// by the service, ie instantiated and not removed.
        /// </summary>
        bool ActorExists (string actorId);
        /// <summary>
        /// Returns a managed actor with specified ID or null when not found.
        /// </summary>
        [CanBeNull] IActor GetActor (string actorId);
        /// <summary>
        /// Adds all the actors currently managed by the service to the specified collection.
        /// </summary>
        void CollectActors (ICollection<IActor> actors);
        /// <summary>
        /// Returns first actor that satisfies the specified filter or null.
        /// </summary>
        [CanBeNull] IActor FindActor (Predicate<IActor> filter);
        /// <summary>
        /// Adds a new managed actor with specified ID.
        /// </summary>
        Awaitable<IActor> AddActor (string actorId);
        /// <summary>
        /// Removes and disposes (destroys) a managed actor with specified ID.
        /// </summary>
        void RemoveActor (string actorId);
        /// <summary>
        /// Removes all the actors managed by the service.
        /// </summary>
        void RemoveAllActors ();
        /// <summary>
        /// Start managing specified actor instance, optionally described by the specified metadata.
        /// </summary>
        /// <remarks>
        /// Use this method to inject a transient actor instance which lifecycle is managed externally.
        /// </remarks>
        void ManageActor (IActor actor, [CanBeNull] ActorMetadata meta = null);
        /// <summary>
        /// Stops managing an actor instance previously registered with <see cref="ManageActor"/>.
        /// Effectively same as <see cref="RemoveActor"/>, but doesn't dispose or destroy the actor.
        /// </summary>
        void UnmanageActor (string actorId);
        /// <summary>
        /// Retrieves state of a managed actor with specified ID.
        /// </summary>
        ActorState GetActorState (string actorId);
        /// <summary>
        /// Retrieves appearance resource loader for actor with specified ID
        /// or null in case actor implementation doesn't require loading resources.  
        /// </summary>
        /// <remarks>
        /// This works even in case actor with specified ID is not currently instantiated
        /// by the manager, as appearance loader lifetime is independent of the associated
        /// actor, which is required to expose appearance resource management to external
        /// entities, such as script commands.
        /// </remarks>
        [CanBeNull] IResourceLoader GetAppearanceLoader (string actorId);
    }

    /// <summary>
    /// Implementation is able to manage <see cref="TActor"/> actors.
    /// </summary>
    /// <typeparam name="TActor">Type of managed actors.</typeparam>
    /// <typeparam name="TState">Type of state describing managed actors.</typeparam>
    /// <typeparam name="TMeta">Type of metadata required to construct managed actors.</typeparam>
    /// <typeparam name="TConfig">Type of the service configuration.</typeparam>
    public interface IActorManager<TActor, TState, TMeta, TConfig> : IActorManager, IEngineService<TConfig>, IStatefulService<GameStateMap>
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>
    {
        /// <inheritdoc cref="IActorManager.AddActor"/>
        new Awaitable<TActor> AddActor (string actorId);
        /// <inheritdoc cref="IActorManager.AddActor"/>
        Awaitable<TActor> AddActor (string actorId, TState state);
        /// <inheritdoc cref="IActorManager.GetActor"/>
        [CanBeNull] new TActor GetActor (string actorId);
        /// <inheritdoc cref="IActorManager.CollectActors"/>
        void CollectActors (ICollection<TActor> actors);
        /// <inheritdoc cref="IActorManager.FindActor"/>
        [CanBeNull] TActor FindActor (Predicate<TActor> filter);
        /// <inheritdoc cref="IActorManager.GetActorState"/>
        new TState GetActorState (string actorId);
    }
}
