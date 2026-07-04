using System;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Thin adapter that lets game code start Fungus blocks (with an optional completion
    /// callback) without every caller depending on Fungus internals. Keeps the third-party
    /// dependency behind one small surface.
    /// </summary>
    public class FungusBridge : MonoBehaviour
    {
        [SerializeField, Tooltip("The Flowchart that owns this scene's narrative blocks.")]
        private Flowchart flowchart;

        /// <summary>Runs a block by name (fire and forget).</summary>
        public void ExecuteBlock(string blockName)
        {
            if (flowchart == null || string.IsNullOrEmpty(blockName))
            {
                return;
            }

            flowchart.ExecuteBlock(blockName);
        }

        /// <summary>
        /// Runs a block by name and invokes <paramref name="onComplete"/> when it finishes.
        /// If the flowchart or block is missing, the callback is invoked immediately so callers
        /// never dead-lock waiting on a block that will not run.
        /// </summary>
        public void ExecuteBlock(string blockName, Action onComplete)
        {
            if (flowchart == null || string.IsNullOrEmpty(blockName))
            {
                onComplete?.Invoke();
                return;
            }

            Block block = flowchart.FindBlock(blockName);
            if (block == null)
            {
                Debug.LogWarning($"[FungusBridge] Block '{blockName}' not found on flowchart '{flowchart.name}'.");
                onComplete?.Invoke();
                return;
            }

            // Fungus invokes onComplete after the block runs its last command (verified in Block.Execute).
            flowchart.ExecuteBlock(block, 0, () => onComplete?.Invoke());
        }
    }
}
