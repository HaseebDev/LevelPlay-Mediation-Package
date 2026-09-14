# Local analytics integration changes

This worktree is based on v1.2.1 commit ddda2f68ea78f21e7de9b0dc355f90e4d9f7b56d. Version 1.2.2 is a local, unpublished package candidate.

AdTelemetry exposes bounded lifecycle actions from the existing rewarded, interstitial and banner controllers without depending on GameAnalytics. Observers cannot prevent other observers, SDK calls or reward callbacks from running. Reward telemetry is emitted once per accepted show even when the native reward callback repeats. AdsManager's existing gameplay API and reward settlement behavior are preserved.

The consumer is Neon Orbit Sorter's com.autech.gameanalytics 0.2.0 package. The source package is built using `npm pack --ignore-scripts`, which honors .npmignore and excludes this repository's Unity development Assets tree. Neon installs the resulting local tarball from Packages/LocalPackages~, not this whole worktree.

Test evidence is in Neon's GameAnalytics pilot receipt: 14 EditMode and 3 PlayMode checks passed after import. Native ad callback delivery and real revenue receipt still require device validation. No Git push or public release has occurred.
