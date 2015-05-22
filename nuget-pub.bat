cd ServiceStackPlugins.Interfaces
del *.nupkg
..\.nuget\nuget pack 
..\.nuget\nuget push *.nupkg
cd ..

cd ServiceStackPlugins.ListReqRespBuilder
del *.nupkg
..\.nuget\nuget pack 
..\.nuget\nuget push *.nupkg
cd ..


cd "ServiceStackPlugins.ListReqRespBuilderAutoMapper"
del *.nupkg
..\.nuget\nuget pack 
..\.nuget\nuget push *.nupkg
cd ..

cd "ServiceStackPlugins.PerFieldAuth"
del *.nupkg
..\.nuget\nuget pack 
..\.nuget\nuget push *.nupkg
cd ..
