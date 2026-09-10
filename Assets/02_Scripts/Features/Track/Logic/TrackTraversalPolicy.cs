namespace ProjectIO.Tracks
{
    public static class TrackTraversalPolicy
    {
        public static TrackTraversalAction Resolve(bool hasNextPath, bool shouldLoopAtFinalPath)
        {
            if (hasNextPath)
            {
                return TrackTraversalAction.TransferToNextPath;
            }

            return shouldLoopAtFinalPath
                ? TrackTraversalAction.LoopToFirstPath
                : TrackTraversalAction.Complete;
        }
    }
}
