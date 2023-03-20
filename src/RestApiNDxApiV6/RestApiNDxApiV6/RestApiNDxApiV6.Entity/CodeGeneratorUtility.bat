::
::
echo off
echo........................................
echo Generate solution code from added Entity classes
echo........................................
echo off
:PROMPT
SET /P AREYOUSURE=Are you sure you want to delete generated files(Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

::Select the VS version
::SET tt="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\TextTransform.exe"
::SET tt="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\TextTransform.exe"
::SET tt="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\TextTransform.exe"
::SET tt="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\TextTransform.exe"
::SET tt="C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\TextTransform.exe"
SET tt="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\TextTransform.exe"

echo off
echo Delete previously generated cs code files 
 DEL /F "1_t4EntityHelpersGenerate.cs"
 DEL /F "..\RestApiNDxApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.cs"	
 DEL /F "..\RestApiNDxApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.cs"	
 DEL /F "..\RestApiNDxApiV6.Domain\Service\4_t4DomainServicesGenerate.cs"	
 DEL /F "..\RestApiNDxApiV6.Api\Controllers\5_t4ApiControllerGenerate.cs"	
 DEL /F "..\RestApiNDxApiV6.Api\5_t4ApiStartupAdditionsGenerate.cs"	
 DEL /F "..\RestApiNDxApiV6.Test\6_t4IntegrationTestGenerate.cs"
echo .
echo Run all T4s...
echo -generate entity helpers
%tt% "1_t4EntityHelpersGenerate.tt" -out "1_t4EntityHelpersGenerate.cs"
echo -generate domain classes
%tt% "..\RestApiNDxApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.tt" -out "..\RestApiNDxApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.cs"
echo -generate mapper classes
%tt% "..\RestApiNDxApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.tt" -out "..\RestApiNDxApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.cs"	
echo -generate services classes
%tt% "..\RestApiNDxApiV6.Domain\Service\4_t4DomainServicesGenerate.tt" -out "..\RestApiNDxApiV6.Domain\Service\4_t4DomainServicesGenerate.cs"	
echo -generate controller classes
%tt% "..\RestApiNDxApiV6.Api\Controllers\5_t4ApiControllerGenerate.tt" -out "..\RestApiNDxApiV6.Api\Controllers\5_t4ApiControllerGenerate.cs"
echo -generate extended Startup code
%tt% "..\RestApiNDxApiV6.Api\5_t4ApiStartupAdditionsGenerate.tt" -out "..\RestApiNDxApiV6.Api\5_t4ApiStartupAdditionsGenerate.cs"
echo -generate Postman json tests
%tt% "..\RestApiNDxApiV6.Test\Postman\t4PostmanTestsGenerate.tt" -out "..\RestApiNDxApiV6.Test\Postman\RestApiN.Postman_tests_collection.json"		
echo -generate test classes
%tt% "..\RestApiNDxApiV6.Test\6_t4IntegrationTestGenerate.tt" -out "..\RestApiNDxApiV6.Test\6_t4IntegrationTestGenerate.cs"	
echo T4s completed.
pause
:END