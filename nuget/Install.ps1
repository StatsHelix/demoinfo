param($installPath, $toolsPath, $package, $project)

# We *could* create a Uninstall.ps1 that automatically removes the assembly from the skip list.
# However, imagine the following scenario: User installs this twice, into projects A and B.
# Then they remove it from project A and work on project B.
# Result: B won't run because uninstalling DemoInfo from A removed the assembly from the skip list.
# So we have no choice, users have to remove it manually.
# While assemblies on the skip list are a security issue, they're not a huge one. Security is
# effectively reduced to straight up loading an unsigned assembly, so we lose nothing.
Host-Write "You are using a prerelease version of DemoInfo. These are built automatically by travis and therefore delay-signed. Because of that, .NET would reject the assembly, so I'm adding it to the verification skip list. You can remove it using 'sn -Vu DemoInfo.dll'. For more information, please have a look at the nuget release notes."
sn -Vr "$installPath\DemoInfo.dll"
