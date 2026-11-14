#!/bin/sh
set -eu
cd "$(dirname "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_USE_POLLING_FILE_WATCHER=1
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
dotnet run --project src/TuneFlow.Web --disable-build-servers -p:UseSharedCompilation=false --urls http://localhost:5080
