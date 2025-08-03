# Release Notes

## 1.0.9

- Added Lookups lazy load values by default
- Added CopyUtil for deep copying
- Added DataTableSelectable columnWidths optional
- Removed useLookupStore and replaced with loading default look label in Lookup.tsx
- Fixed delete dependency finder with extended entities
- Fixed import data console logs error messages
- Fixed Postgres import data command timeout too high
- Fixed cascade deletes not setting entities with multiple null values
- Fixed data form width when formMinWidth specified
- Fixed On the DataTable if looking at Inactive records, then click one of the columns to sort, it shows Active records instead of Inactive
- Fixed DataTableSelectable not using primary key to determine selected record. It’s currently using name of table + Id
- Fixed DataTableSelectable headers layout misalignment
- Fixed LookupDescription render empty string instead of “null”
- Fixed Lookup fields default to searching active records
- Fixed Calling update without providing the primary key field doesn’t return a descriptive error
- Resolved all Dependabot security issues

## 1.0.8

- Added whoami endpoint
- Added Development tab in dashboard
- Added Types in development tab to generate typescript types
- Added addUserIdUrlParam client utility method
- Added service logic & actions works with dependency injection
- Added lookups search on description
- Added display attributes for BaseEntity fields
- Added formMinSize to DataTable
- Added select columns on datatable
- Added ExecuteJob to BaseServiceContext
- Added Job Parameters
- Fixed data export
- Fixed data import uses scheduled jobs to handle larger imports
- Fixed hide discriminator columns from tables\reads
- Fixed sqlite concurrency deadlocks
- Fixed sqlite null non-nullable fields
- Fixed return primary key as field in metadata
- Fixed FormContainer uses form
- Fixed admin dash shows Active\Inactive switch by default
- Moved Post\Pre save events to FormContainer

## 1.0.7

- Core entities now internally located
- Xams.Core now able to query core entities
- Create Api’s from Program.cs with .AddXamsApis()
- Serve dashboard using AddXamsApis option
- Add services from Program.cs with .AddXamsServices()

## 1.0.6

- Support for numerical and custom keys
- Support for all c# data types on Entities
- Tables no longer require the Table attribute
- Entities no longer require a Name property or UIName attribute
- Significant permission caching performance increase
- Permissions caching enabled by default
- Delete PostOrderTraversal parallelized for performance increase
- Parallelized PreEntity fetching
- Disabled query change tracking for DbContext
- Deletes now take 15% of the previous deletion time
- Fixed Delete retrieves available records in current transaction

## 1.0.5

- Added JobTimeZone attribute to adjust job scheduled time for a specific timezone
- Improved console logging for Scheduled Jobs
- Fixed manual triggering of jobs from admin dashboard
- Fixed DataTable refreshInterval
- Fixed DataGrid column sizes not maintaining on change of snap columns\rows

## v1.0.4

- Added multi-server capabilities
- Added "Server" to admin dashboard
- Added JobServer attribute for specifying which server a job should execute on

## 1.0.3

- Fixed unable to create Roles with SQLite

## 1.0.2

- Created "ExecutionId" in contexts to uniquely identify each transaction
- Role Permissions are deleted if they aren't contained within the import
- Fixed attempting to delete record in service logic slated for delete by PostTraversalDelete DbConcurrencyException
- Fixed unable to set DateTime field without UIDateFormat
- Fixed UIDateFormat in DataTable not working with NextJS static site export
- Fixed Delete on DataTable when Delete Permission at User or Team level
- Fixed Delete on DataTable when not requesting Owner fields
- Fixed DataTable ref.refresh closing form
- Fixed DateTime fields not returning in UTC with SQL Server
- Fixed DateTime field searching on year, day and month
- Added entity Id's to admin dashboard
- Added DataGrid Cell onEditing prop to provide cell value filtering
- Added allowNegative prop to Field component to prevent negative numbers
- Added UINumberRange attribute

## 1.0.1

- Added parameter to UIHide to make hidden fields optionally queryable
- Fixed UIDateTime format attribute not working properly with Mantine DateTime component
- Fixed permissions to query database if a user's permission aren't found in the cache
- Fixed DataService not retrieving all permissions for a table for a given operation
- Fixed OwningUser and OwningTeam fields populating when there are no assign permissions
- Fixed Admin Dashboard closing modal on create of Role
- Fixed prevent delete of Audit Field records from the Admin Dashboard
- Fixed "undefined" listed in the quicksearch dropdown when a custom column is used
- Fixed Lookup filtered to only 1 item after an item has been selected
- Fixed React duplicate key error in Admin Dashboard
