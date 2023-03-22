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
 DEL /F "..\RestApiNExApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.cs"	
 DEL /F "..\RestApiNExApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.cs"	
 DEL /F "..\RestApiNExApiV6.Domain\Service\4_t4DomainServicesGenerate.cs"	
 DEL /F "..\RestApiNExApiV6.Api\Controllers\5_t4ApiControllerGenerate.cs"	
 DEL /F "..\RestApiNExApiV6.Api\5_t4ApiStartupAdditionsGenerate.cs"	
 DEL /F "..\RestApiNExApiV6.Test\6_t4IntegrationTestGenerate.cs"
echo .
echo Run all T4s...
echo -generate entity helpers
%tt% "1_t4EntityHelpersGenerate.tt" -out "1_t4EntityHelpersGenerate.cs"
echo -generate domain classes
%tt% "..\RestApiNExApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.tt" -out "..\RestApiNExApiV6.Domain\Domain\2_t4DomainViewModelsGenerate.cs"
echo -generate mapper classes
%tt% "..\RestApiNExApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.tt" -out "..\RestApiNExApiV6.Domain\Mapping\3_t4DomainMappingProfileGenerate.cs"	
echo -generate services classes
%tt% "..\RestApiNExApiV6.Domain\Service\4_t4DomainServicesGenerate.tt" -out "..\RestApiNExApiV6.Domain\Service\4_t4DomainServicesGenerate.cs"	
echo -generate controller classes
%tt% "..\RestApiNExApiV6.Api\Controllers\5_t4ApiControllerGenerate.tt" -out "..\RestApiNExApiV6.Api\Controllers\5_t4ApiControllerGenerate.cs"
echo -generate extended Startup code
%tt% "..\RestApiNExApiV6.Api\5_t4ApiStartupAdditionsGenerate.tt" -out "..\RestApiNExApiV6.Api\5_t4ApiStartupAdditionsGenerate.cs"
echo -generate Postman json tests
%tt% "..\RestApiNExApiV6.Test\Postman\t4PostmanTestsGenerate.tt" -out "..\RestApiNExApiV6.Test\Postman\RestApiN.Postman_tests_collection.json"		
echo -generate test classes
%tt% "..\RestApiNExApiV6.Test\6_t4IntegrationTestGenerate.tt" -out "..\RestApiNExApiV6.Test\6_t4IntegrationTestGenerate.cs"	
echo -add new db migration
%tt% "t4_AddMigration.tt" -out "t4_AddMigration.cs"
echo T4s completed.
pause
:END