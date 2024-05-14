﻿namespace FocusedMetaActions.Train.PreconditionAdditionRefinements.Heuristics
{
    public class hMostValid : IHeuristic
    {
        public int GetValue(PreconditionState preconditions)
        {
            return 100 - (int)((1 - ((double)preconditions.InvalidStates / (double)preconditions.TotalInvalidStates)) * 100);
        }
    }
}