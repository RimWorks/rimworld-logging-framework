.PHONY: help all clean restore build build-core test format lint pack

CONFIG ?= Debug
SLN := RimWorks.RimLogging.slnx
BUILD_ARGS ?=

help:
	@echo "Targets:"
	@echo "  all              restore + build whole solution"
	@echo "  clean            remove bin/, obj/, and the version output folders"
	@echo "  restore          dotnet restore"
	@echo "  build            build whole solution"
	@echo "    build once per version loadFolders.xml declares"
	@echo "  build-core       build only RimWorks.RimLogging"
	@echo "  test             run xunit suites"
	@echo "  format           dotnet format"
	@echo "  lint             dotnet format --verify-no-changes"
	@echo "  pack             create nuget packages (Release)"

all: restore build

clean:
	rm -rf [0-9].[0-9]
	rm -rf Source/*/bin Source/*/obj

restore:
	dotnet restore $(SLN)

build:
	dotnet build $(SLN) -c $(CONFIG) --nologo

# loadFolders.xml is the one version list; a second copy here or in release.config.mjs drifts
build-core:
	dotnet build Source/RimWorks.RimLogging/RimWorks.RimLogging.csproj -c $(CONFIG) --nologo

test:
	dotnet test $(SLN) -c $(CONFIG) --nologo

format:
	dotnet format $(SLN)

lint:
	dotnet format $(SLN) --verify-no-changes

pack:
	dotnet pack Source/RimWorks.RimLogging/RimWorks.RimLogging.csproj -c Release --nologo -o out/nupkg
