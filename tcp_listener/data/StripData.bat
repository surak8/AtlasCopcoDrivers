@echo on
setlocal
set FNAME=IRData
set ORIG=%~dp0%FNAME%.txt
set TMP=%~dp0%FNAME%.tmp
set SED=%~dp0%FNAME%.sed
set SED2=%~dp0%FNAME%.2.sed
set CSV0=%~dp0%FNAME%.0.csv
set CSV=%~dp0%FNAME%.csv
findstr -a:c -c:"parsed the string" %ORIG% > %TMP%
sed -f %SED% %TMP% > %CSV0%
REM sort %CSV0% | uniq | sed -f %SED2% > %CSV%
copy /y %CSV0% %CSV%
del /q %TMP% %CSV0% 
