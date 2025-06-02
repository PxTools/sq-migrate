# sq-migrate
Tool for saved queries you can migrate old saved queries to saved queries used in `PxWebApi2`
You can also generat statistics from you saved queries before migrating.

## Migrate
You can migrate saved queries store on disk to a new location on disk or saved queries stored in database in table `SavedQueryMeta`into table `SavedQueryMeta2`

### File based saved queries

Run the following command replace the placeholders with your parameters

```powershell
./sq-migrate.exe migrate -t File -x SOURCE_TYPE -s "MY_SOURCE" -d "MY_DESTIONATION" -p DATABASE_ID
```

Replace the following placeholders
- SOURCE_TYPE if you have your data in PX files write `px` if you have your data in the Common Nordic Meta Model write `cnmm`
- MY_SOURCE should be the path to where your have your saved queries.
- MY_DESTINATION should be the path to where to store the converted saved queries. This should be a diffrent folder from `MY_SOURCE`.
- DATABASE_ID this sould be the path to the `Menu.xml` file and all the PX files.

### Database based saved queries

Run the following command replace the placeholders with your parameters

```powershell
./sq-migrate.exe migrate -t Database -x SOURCE_TYPE -s "MY_SOURCE" -p DATABASE_ID -v MY_VENDOR
```

Replace the following placeholders
- SOURCE_TYPE if you have your data in PX files write `px` if you have your data in the Common Nordic Meta Model write `cnmm`
- MY_SOURCE Should be a connection string to the database where the `SavedQuerMeta` and `SavedQuerMeta2`
- DATABASE_ID this sould be the database id in  `SqlDb.config`.
- MY_VENDOR should be `mssql` or `oracle` depending on your database vendor.


## Generate statistics

### Exampel for generating PX file based database

Run the following command

```powershell
./sq-migrate.exe stats stats -l MY_SOURCE
```
Replace  `MY_SOURCE` with the path to your saved queries.