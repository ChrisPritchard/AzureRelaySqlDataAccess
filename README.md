# AzureRelaySqlDataAccess
Uses a relay listener and a client library to forward database requests through an Azure Relay Hybrid Connection. Useful for accessing databases that are behind firewalls, for development purposes. E.g. building a site or app that needs access to the database, but where your development machine is outside the secure network.

Built to call stored procedures against Sql Server, expecting a set of rows back. Should be easily generalisable to other scenarios or database engines (in theory - exercise is left to the reader).

Built in .NET Core (NetStandard, NetCoreApp), but should be possible to convert for earlier frameworks.

Use:
- The RelayForwarder project is a class library you include in your client application. Instantiate a RelayClient with the correct configuration, then when needing to make a database call invoke the ForwardToRelay method
- Before the RelayClient will work, deploy the build output of the RelayListenerDbCaller project to a location that can access the Db, configuring correctly in its appsettings (conn string, relay config etc). Run the project and leave it.
- When a call is made, the listener will print output showing whats happening