@echo off
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
set DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
dotnet run --project src/TuneFlow.Web --disable-build-servers -p:UseSharedCompilation=false --urls http://localhost:5080
