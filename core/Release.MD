# Release Notes

## v1.0.1

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
