using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    /// <summary>
    /// Tracks if the attached entity is moving, and how it can be moved.
    /// </summary>
    [GenerateAuthoringComponent]
    public struct CharacterControllerComponentData : IComponentData
    {
        // -------------------------------------------------------------------------------------
        // Current Movement
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The current direction that the character is moving.
        /// </summary>
        public float3 CurrentDirection { get; set; }

        /// <summary>
        /// The current magnitude of the character movement.
        /// If <c>0.0</c>, then the character is not being directly moved by the controller but residual forces may still be active.
        /// </summary>
        public float CurrentMagnitude { get; set; }

        /// <summary>
        /// Is the character requesting to jump?
        /// Used in conjunction with <see cref="IsGrounded"/> to determine if the <see cref="JumpStrength"/> should be used to make the entity jump.
        /// </summary>
        public bool Jump { get; set; }

        // -------------------------------------------------------------------------------------
        // Control Properties
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Gravity force applied to the character.
        /// </summary>
        public float3 Gravity;

        /// <summary>
        /// The current speed at which the player moves.
        /// </summary>
        public float Speed;

        /// <summary>
        /// The jump strength which controls how high a jump is, in conjunction with <see cref="Gravity"/>.
        /// </summary>
        public float JumpStrength;

        /// <summary>
        /// The maximum height the character can step up, in world units.
        /// </summary>
        public float MaxStep;

        /// <summary>
        /// Drag value applied to reduce the <see cref="VerticalVelocity"/>.
        /// </summary>
        public float Drag;

        // -------------------------------------------------------------------------------------
        // Control State
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// True if the character is on the ground.
        /// </summary>
        public bool IsGrounded { get; set; }

        /// <summary>
        /// The current horizontal velocity of the character.
        /// </summary>
        public float3 HorizontalVelocity { get; set; }

        /// <summary>
        /// The current jump velocity of the character.
        /// </summary>
        public float3 VerticalVelocity { get; set; }
    }
}
