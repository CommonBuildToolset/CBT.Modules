# How to release modules

1. Create a GitHub release
   * Building localy to get version of package being released.
   Create release with tag and release name of Package.Version.  Example: CBT.Nuget.1.0.110
2. Add list of changes that are customer facing that have been fixed for the module to the release description.
3. Publish release and wait for the AppVeyor build to complete
   * navigate to https://www.appveyor.com/ and login to CBT.  Select from artifacts the nupkg and it's symbol nupkg if any.
4. Push the nupkg from the build
5. Attach the nupkg to the GitHub release
6. Update CBT.Examples repo to include any new functionality

## Automate this!
