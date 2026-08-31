.PHONY: help all clean restore build build-core test format lint pack

CONFIG ?= Debug
SLN := RimWorks.RimLogging.sln

help:
	@echo "Targets:"
	@echo "  all              restore + build whole solution"
	@echo "  clean            remove bin/, obj/, Assemblies/"
	@echo "  restore          dotnet restore"
	@echo "  build            build whole solution"
	@echo "  build-core       build only RimWorks.RimLogging"
	@echo "  test             run xunit suites"
	@echo "  format           dotnet format"
	@echo "  lint             dotnet format --verify-no-changes"
	@echo "  pack             create nuget packages (Release)"

all: restore build

clean:
	rm -rf Assemblies/*.dll Assemblies/*.pdb Assemblies/*.xml
	rm -rf RimWorks.RimLogging/bin RimWorks.RimLogging/obj
	rm -rf RimWorks.RimLogging.Tests/bin RimWorks.RimLogging.Tests/obj

restore:
	dotnet restore $(SLN)

build:
	dotnet build $(SLN) -c $(CONFIG) --nologo

build-core:
	dotnet build RimWorks.RimLogging/RimWorks.RimLogging.csproj -c $(CONFIG) --nologo

test:
	dotnet test $(SLN) -c $(CONFIG) --nologo

format:
	dotnet format $(SLN)

lint:
	dotnet format $(SLN) --verify-no-changes

pack:
	dotnet pack RimWorks.RimLogging/RimWorks.RimLogging.csproj -c Release --nologo -o out/nupkg
