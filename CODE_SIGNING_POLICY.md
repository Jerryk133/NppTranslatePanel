# Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Status and scope

The SignPath Foundation application for NppTranslatePanel is being prepared. Until the application is approved and the automated signing workflow is enabled, release artifacts are unsigned and must not be described as signed.

Once enabled, this policy applies to the `NppTranslatePanel.dll` files distributed for x64 and x86 Notepad++. The DLLs are signed before they are placed in the release ZIP archives. Checksums are generated only after signing and packaging.

## Source and builds

- Source repository: <https://github.com/Jerryk133/NppTranslatePanel>
- Release page: <https://github.com/Jerryk133/NppTranslatePanel/releases>
- License: [Apache License 2.0](LICENSE.md)
- Automated build system: GitHub Actions on GitHub-hosted Windows runners
- Build workflow: [`.github/workflows/build.yml`](.github/workflows/build.yml)

Official signed artifacts must be produced from the public repository by the automated build workflow. Locally built or manually modified binaries are not eligible for release signing. The source revision, workflow run, version tag, unsigned build artifact, signing request, signed artifact, and published checksums must remain traceable to each other.

## Signing and release rules

Release-signing requests are permitted only when all of the following are true:

1. The source revision is identified by a version tag in the form `vMAJOR.MINOR.PATCH`.
2. The tag version matches the plugin assembly and package version.
3. The build and test workflow completed successfully on GitHub-hosted runners.
4. The signing input is the workflow artifact produced by that same run.
5. A signing approver has reviewed the release and explicitly approved the request.
6. Authenticode signatures and timestamps are verified before packaging and publication.
7. SHA-256 checksums are calculated from the final signed ZIP archives.

The signing credential and private key must never be stored in this repository or exposed to the build job. SignPath holds the signing key in its managed signing service. GitHub and SignPath access tokens must be stored only as encrypted repository secrets with the minimum required permissions.

The build workflow already publishes a dedicated unsigned signing-input artifact. After SignPath approves the project, the SignPath GitHub App and signing action will be connected to that artifact using the organization ID, project slug, signing-policy slug, artifact-configuration slug, and API token issued for the project. No placeholder credentials are committed to the repository.

## Team roles

- Committer and reviewer: [Jerryk133](https://github.com/Jerryk133)
- Signing approver: [Jerryk133](https://github.com/Jerryk133)

Changes proposed by contributors without direct commit access require review by the maintainer before merge. Accounts with repository or signing access must use multi-factor authentication.

## Incident response

If a signing credential, workflow, release artifact, or maintainer account may have been compromised, release signing is suspended until the incident is investigated. Affected releases are removed or clearly marked, users are notified through the GitHub release or security advisory, and SignPath is contacted when certificate revocation or another signing response may be required.

## Privacy

The signing service receives release binaries and build provenance, not users' documents or translation credentials. Runtime data handling by the plugin is described in the [privacy policy](PRIVACY.md).
