CSC=csc.exe
RES=gorc.exe
OBJS=vbservice.res

default: all

all: $(OBJS) vbservice

vbservice:	vbservice.cs vbserviceinstaller.cs vbox.cs
		$(CSC) /win32res:vbservice.res $^

vbservice.res:	vbservice.rc
		$(RES) /r $<
