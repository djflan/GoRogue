﻿using System.Collections.Generic;
using JetBrains.Annotations;

namespace GoRogue.MapGeneration
{
    /// <summary>
    /// Map generator that applies a series of <see cref="GenerationStep" /> instances to a <see cref="GenerationContext" />
    /// to generate a map.
    /// </summary>
    [PublicAPI]
    public class Generator
    {
        private readonly List<GenerationStep> _generationSteps;

        /// <summary>
        /// Context for the map this <see cref="Generator" /> is generating.
        /// </summary>
        public readonly GenerationContext Context;

        /// <summary>
        /// Creates a generator that will be used to generate a map of the given width/height.
        /// </summary>
        /// <param name="width">Width of the generated map.</param>
        /// <param name="height">Height of the generated map.</param>
        public Generator(int width, int height)
        {
            Context = new GenerationContext(width, height);
            _generationSteps = new List<GenerationStep>();
        }

        /// <summary>
        /// Steps used to generate the map.
        /// </summary>
        public IReadOnlyList<GenerationStep> GenerationSteps => _generationSteps.AsReadOnly();

        /// <summary>
        /// Adds a component to the context this generator is applying generation steps to.
        /// </summary>
        /// <param name="component">Component to add to the map context.</param>
        /// <param name="tag">An optional tag to give the component.  Defaults to no tag.</param>
        /// <returns>This generator (for chaining).</returns>
        public Generator AddComponent(object component, string? tag = null)
        {
            Context.AddComponent(component, tag);
            return this;
        }

        /// <summary>
        /// Adds a generation step.  Steps are executed in the order they are added.
        /// </summary>
        /// <param name="step">The generation step to add.</param>
        /// <returns>This generator (for chaining).</returns>
        public Generator AddStep(GenerationStep step)
        {
            _generationSteps.Add(step);
            return this;
        }

        /// <summary>
        /// Adds the given generation steps.  Steps are executed in the order they are added.
        /// </summary>
        /// <param name="steps">The generation steps to add.</param>
        /// <returns>This generator (for chaining).</returns>
        public Generator AddSteps(params GenerationStep[] steps) => AddSteps((IEnumerable<GenerationStep>)steps);

        /// <summary>
        /// Adds the given generation steps.  Steps are executed in the order they are added.
        /// </summary>
        /// <param name="steps">The generation steps to add.</param>
        /// <returns>This generator (for chaining).</returns>
        public Generator AddSteps(IEnumerable<GenerationStep> steps)
        {
            _generationSteps.AddRange(steps);
            return this;
        }

        /// <summary>
        /// Applies the generation steps added, in the order in which they were added. to the <see cref="Context" /> to generate
        /// the map.
        /// </summary>
        /// <returns>This generator (for chaining).</returns>
        public Generator Generate()
        {
            foreach (var step in _generationSteps)
                step.PerformStep(Context);

            return this;
        }
    }
}
