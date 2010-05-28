CSC=csc.exe
RES=gorc.exe

default: all

all: vbservice

vbservice:	vbservice.cs vbserviceinstaller.cs vbox.cs
		$(CSC) /win32res:vbservice.res $^

vbservice.res:	vbservice.rc
		$(RES) /r $<
