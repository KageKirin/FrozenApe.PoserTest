#
# Copyright 2022~ Christian Helmich. All rights reserved.
# License: http://www.opensource.org/licenses/BSD-2-Clause
#
SHELL := zsh

UNAME := $(shell uname)
ifeq ($(UNAME),$(filter $(UNAME),Linux Darwin SunOS FreeBSD GNU/kFreeBSD NetBSD OpenBSD GNU))
ifeq ($(UNAME),$(filter $(UNAME),Darwin))
HOST_OS=darwin
TARGET_OS?=darwin
else
ifeq ($(UNAME),$(filter $(UNAME),SunOS))
HOST_OS=solaris
TARGET_OS?=solaris
else
ifeq ($(UNAME),$(filter $(UNAME),FreeBSD GNU/kFreeBSD NetBSD OpenBSD))
HOST_OS=bsd
TARGET_OS?=bsd
else
HOST_OS=linux
TARGET_OS?=linux
endif
endif
endif
else
EXE=.exe
HOST_OS=windows
TARGET_OS?=windows
endif

APP_NAME?=test

UNITY_VERSION?=6000.0.38f1

ifeq ($(HOST_OS),darwin)
UNITY?=/Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/MacOS/Unity
else
ifeq ($(HOST_OS),linux)
UNITY?=/Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/Linux/Unity
else
ifeq ($(HOST_OS),windows)
UNITY?="C:\\Program Files\\Unity\\Hub\\Editor\\$(UNITY_VERSION)\\Editor\\Unity.exe"
endif
endif
endif

ifeq ($(TARGET_OS),darwin)
TARGET_APP?=$(APP_NAME).app
TARGET_EXE?=./$(TARGET_APP)/Contents/MacOS/FrozenApe.PoserTest
TARGET_ARG?=-buildOSXUniversalPlayer $(TARGET_APP)
TARGET_ARG2?=-buildTarget OSXUniversal -buildOutputPath $(TARGET_APP)
else
ifeq ($(TARGET_OS),linux)
TARGET_APP?=$(APP_NAME)
TARGET_EXE?=./$(TARGET_APP)
TARGET_ARG?=-buildLinux64Player $(TARGET_APP)
TARGET_ARG2?=-buildTarget Linux64 -buildOutputPath $(TARGET_APP)
else
ifeq ($(TARGET_OS),windows)
TARGET_APP?=$(APP_NAME).exe
TARGET_EXE?=./$(TARGET_APP)
TARGET_ARG?=-buildWindows64Player $(TARGET_APP)
TARGET_ARG2?=-buildTarget Win64 -buildOutputPath $(TARGET_APP)
else
ifeq ($(TARGET_OS),webgl)
TARGET_APP?=$(APP_NAME).web
TARGET_EXE?=./$(TARGET_APP)
TARGET_ARG?=-buildTarget WebGL -buildOutputPath $(TARGET_APP)
TARGET_ARG2?=-buildTarget WebGL -buildOutputPath $(TARGET_APP)
endif
endif
endif
endif

TRACE?=0
ifeq ($(TRACE),0)
TRACE_ARG=
else
TRACE_ARG=-enablePackageManagerTraces
endif

SILENT?=0
ifeq ($(SILENT),0)
SILENT_BUILD=build-verbose
else
SILENT_BUILD=build-silent
endif

MODE?=0
ifeq ($(MODE),2)
TARGET_ARG=$(TARGET_ARG2)
endif


$(TARGET_APP): build

build-help: 
	$(UNITY) --help -nographics -batchmode $(TRACE_ARG) -quit

b build: $(SILENT_BUILD)

build-verbose: refresh-token clean
	$(UNITY) -projectPath $(PWD) -logFile -  $(TARGET_ARG) -nographics -batchmode $(TRACE_ARG) -quit | tee build.log
	@cat build.log | rg -s warning || echo 'no warnings in log'
	@cat build.log | rg -s error || echo 'no errors in log'
	@ls -alG $(TARGET_APP)

build-silent: refresh-token clean
	$(UNITY) -projectPath $(PWD) -logFile unity.log  $(TARGET_ARG) -nographics -batchmode $(TRACE_ARG)  | tee build.log
	@cat unity.log | rg -s warning || echo 'no warnings in unity.log'
	@cat unity.log | rg -s error || echo 'no errors in unity.log'
	@cat build.log | rg -s warning || echo 'no warnings in build.log'
	@cat build.log | rg -s error || echo 'no errors in build.log'
	@ls -alG $(TARGET_APP)

c clean:
	rm -rf $(TARGET_APP)

GCPNPM=$(shell jq -r '.scopedRegistries[] | select(.url | contains("pkg.dev")) | .url' Packages/manifest.json)
refresh-token:
	echo "Refreshing token for $(GCPNPM)"
	$(foreach url,$(GCPNPM), GCPRefreshUpmToken -r $(url) -c ~/.upmconfig.toml)

r run: $(TARGET_APP)
	$(TARGET_EXE) -logFile - -batchmode -nographics

rng run-no-graphics: $(TARGET_APP)
	$(TARGET_EXE) -logFile - -batchmode -nographics

rg run-gui: $(TARGET_APP)
	$(TARGET_EXE) -logFile -


help: $(TARGET_APP)
	$(TARGET_EXE) -logFile - -batchmode -nographics --help

f format:
	@fd . -td --max-depth 1 --search-path Packages/ --search-path Assets/ | xargs -P 8 -I _ sh -c "echo _; cd _; fd '\.cs$$' -tf -X dotnet csharpier {}; fd '\.cs$$' -tf -X dos2unix -q -r {};"
	@fd csproj$$ -X dos2unix -q -r {}
