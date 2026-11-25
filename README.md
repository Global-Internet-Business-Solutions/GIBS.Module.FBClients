# GIBS.Module.FBClients

## FBClients Import Data

The Import Data screen provides read-only preparation and reconciliation. It discovers recognized `dbo` client and history tables, saves a selection, and runs preflight against a chosen FBClients module on the current site. A separate bounded rehearsal write path is disabled by default and can only target a host-configured isolated module. Inventory and `GIBS_FBClientsVisitItem` remain excluded from the original client run; use the separate inventory/ordering selection below.

Preflight uses the **saved** source, table selection, and explicit DNN source `ModuleID`. Enter **412** for this migration. For an existing selection, click **Save Source ModuleID** to preserve its table selection while saving the scope (the server rechecks table availability). For a new selection, inspect the tables and save the selection. Enter destination FBClients module **33**, then click **Run Preflight**. Missing destination or saved scope values produce an actionable message rather than a silently disabled button. Existing plans without a source ModuleID cannot run preflight. Clients are filtered by their source `ModuleID`; all six active child/history tables are filtered through their source client. Counts, source ID validation, collision checks, and Christmas classifications use this same scope. Clients belonging to module 423 and their history are excluded. Destination collisions remain tenant-wide, not restricted to destination module 33. Missing or invalid source scope metadata and a module with no clients prevent preflight; multiple portal values within the selected module remain a blocker.

Preflight checks mapped integer ID columns, validates source IDs by streaming for null, nonpositive, and duplicate values, and requires destination single-column identity primary-key metadata. A declared source primary key is not required, including for the legacy Christmas table. Missing tables may also indicate insufficient metadata visibility. Scoped merged-table mappings and duplicate/reimport checks remain unimplemented; selected merged tables report blockers and no unfiltered counts. Omit them for the first import.

When active child tables are selected, preflight also compares all scoped source client IDs with destination clients. It warns about missing parents and blocks IDs owned by another destination module. This is a point-in-time ownership check, not proof of imported provenance; the future importer must recheck parent identity and ownership transactionally, including Christmas family relationships, before writing children.

**Run Reconciliation** compares selected active-table key sets, destination-module ownership, parent/family relationships, mapped values, and generated audit policy against the same saved source scope. It reports source and destination counts, missing and extra keys, mismatches, and source-wide orphans separately. Parentless Christmas rows and the 96 reviewed parentless turkey rows for ModuleID 412 are approved exclusions; other source orphans require review. Unowned destination rows are tenant-wide. Mapped values are compared after the approved transformations; generated audit fields are checked for consistency, not provenance. Merged tables are not supported. The comparison is read-only and point-in-time; it does not establish row provenance or verify trigger side effects, and is not approval for import or cutover. Use a consistent source snapshot and repeat after changes.

Each active table now has an expandable field mapping report. Matching field names copy without trimming or null substitution; explicit rules map primary/parent IDs and all legacy audit fields. Destination ModuleId is generated from the chosen Oqtane module, while the original DNN ModuleID is retained separately. Oqtane audit names use the authenticated importing administrator and audit timestamps use UTC import time; original DNN audit values remain in legacy fields. Missing, incompatible, and unmapped source/destination columns block preflight. Scoped aggregate queries check required nulls, string/binary lengths (including trailing spaces), varchar encoding round trips using destination column collation, numeric overflow and decimal rounding, and date conversion failures. Date-only destinations intentionally retain only the calendar date. Audit timestamps are checked by conversion to the destination type and back to the original source type, preserving source precision without falsely treating SQL Server datetime's fractional-second representation as data loss. Actual range or source-precision loss still blocks import. Conditional Christmas family links are checked separately by the preservation policy. Preflight describes generated audit values without executing them; reconciliation checks that generated audit fields are present and internally consistent, but does not prove which administrator wrote them.


Approved exception: if source `GIBS_FBClients.PrimaryIncomeSource` is absent, map destination `PrimaryIncomeSource` to an empty string and report a warning. A read-only column probe confirms absence rather than treating missing metadata as proof; permission failures still surface as errors. If the source column exists, preserve and validate its values normally, including required-null and length checks; do not infer values from income history. Preflight describes this generation policy but does not write data.

The experimental batch writer is exposed only through a host-configured, destination-restricted rehearsal API and UI; it remains disabled by default and is not production import approval. It requires an initially empty destination module and either an online native SQL Server snapshot with visible identity metadata or the explicitly approved frozen Azure SQL copy mode described below.

It rejects existing source-key collisions, preserves IDs, processes parents first, and commits each batch and checkpoint together. Checkpoints bind the source snapshot, scope, ordered mapping, audit user, batch size, and policy version; changes require a new destination or an explicitly reviewed recovery. The checkpoint row is serialized by destination module, so only one batch advances at a time; resumption must use the same import module, snapshot, table selection, policy, and audit identity. A committed batch remains durable after interruption; a failed batch and its checkpoint update roll back together. No reset or checkpoint-deletion operation is provided. Destination preflight blocks disabled/untrusted check or foreign-key constraints, disabled unique indexes, and disabled triggers; the writer uses `CheckConstraints | FireTriggers` and rechecks this metadata in each batch transaction. This metadata check does not validate trigger side effects, arbitrary unique-key conflicts, or provenance; only a representative isolated rehearsal can do that. Mapped-value reconciliation compares source-mapped fields, approved generated values, and audit-field consistency for matched destination rows. A synthetic, isolated SQL Server rehearsal exercised all seven active tables, rollback, resume, retry and key/relationship reconciliation; it is not a rehearsal of the actual DNN data.
Upgrading the FBClients package to **1.0.3** registers migration `01000300_InitializeImportCheckpoints`; verify the Oqtane upgrade separately in isolation.

### Separate inventory and ordering rehearsal

The **Separate inventory and ordering import** panel supports `GIBS_FBProductCategory`, `GIBS_FBProducts`, `GIBS_FBSuppliers`, `GIBS_FBInvoices`, `GIBS_FBLineItems`, and `GIBS_FBClientsVisitItem`. Save its selection independently of the original client plan. Tables can be skipped for each source; selecting products includes categories, selecting invoices includes suppliers, selecting invoice items includes products and invoices (and their parents), and selecting visit items includes products and categories. Automatically included missing tables block saving. Empty selected tables are supported and advance progress; skipped tables are neither imported nor reconciled.

Enter the source inventory and client ModuleIDs and the destination Inventory and FBClients ModuleIDs explicitly. For the reviewed source these are inventory **416 -> 35** and clients **412 -> 33**; they are not hardcoded defaults for other sources. Root inventory tables are scoped by source inventory ModuleID; invoice items through their invoice and visit items through their visit/client. Source-wide items without an attributable parent are counted separately and block the run: the Christmas/turkey orphan exclusions do not apply. Missing positive category/supplier/product parents and cross-module inventory relationships also block. Category ID `0` becomes `NULL`, as does an already-null category; no other missing parent is silently detached or skipped.

Primary IDs are preserved with `KeepIdentity`, and the corresponding `Legacy...Id` fields retain them. Parent IDs therefore remain the same, only after source relationship validation, tenant-wide collision checks, and transactional destination parent/legacy/module checks. Selected destination tables must be fresh for their owner module; existing test records block import. No truncate, cleanup, merge, upsert, or checkpoint reset is provided. Review any test-data cleanup independently before rehearsal. Inventory source ModuleID is retained in the immutable run identity rather than copied into destination `ModuleId`; portal and original audit values are retained in their legacy columns. Unmapped columns or type/length/encoding/precision loss block rather than silently dropping data.

The only missing-column exception is `dbo.GIBS_FBLineItems.GIBS_FBLineItems`: if its absence is confirmed with table-level SELECT permission and the destination `GIBSFBLineItems` field is nullable decimal or numeric, preflight warns and the importer generates `NULL`. Present source values, including nulls, are preserved; incompatible types and uncertain permissions are not treated as absence. Reconciliation checks the generated nulls. Line-item runs bind the column's presence to their checkpoint identity; runs started under the previous line-item policy cannot resume under this policy and require review, not checkpoint deletion. Checkpoint identities for selections without line items are unchanged.

Visit items additionally require the original saved plan to contain clients and visits for the same source and client scope, and its completed `1main` checkpoint must match its frozen-source identity and administrator. Clients/visits are verified and reused, not reimported; if this prerequisite is missing, finish the original rehearsal first. Frozen Azure verification covers all selected dependency tables and, for visit items, the original plan's fingerprint tables, with original and combined identities rechecked before commit. Each pass reads shared tables once and feeds the same data into both fingerprints; there is one pass before validation and another before commit. The original fingerprint format and checkpoint identities are unchanged. No original-source scans are needed for inventory-only selections. This is not a substitute for keeping the source frozen, and actual savings depend on source and network performance.

Before enabling writes, configure `FBClientsImport:Rehearsal:InventoryModuleId` to the isolated Inventory module (35 for this rehearsal), in addition to the existing `Enabled`, `DestinationModuleId`, and frozen-source settings. The server verifies both destinations' module types and site, and enforces the host-approved IDs on every write request. Install the changed client/shared/server binaries deliberately in the isolated site; this feature reuses the existing inventory schema and `RunKey` checkpoint migration and needs no new table migration. Do not run the package post-build deployment script as part of verification.

Use **Inventory preflight**, resolve blockers, then **Run/Resume Separate Inventory Rehearsal**. One request commits up to 25 logical 100-row batches from one table with its checkpoint; repeat until complete, including zero-row tables. A failed chunk rolls back its rows and progress; committed chunks can be resumed using the same saved plan, source, administrator, and batch size. The live timer includes verification/network time, not cumulative import duration. Use **Inventory reconciliation** after completion to compare scoped IDs, owner and parent relationships, preserved field values, and generated-audit consistency. Reports do not establish provenance, trigger side effects, or production approval.

The inventory checkpoint is keyed by **destination Inventory module** and an explicitly saved numbered key, initially `3inventory` (for this rehearsal, `(35, 3inventory)`). Existing `(33, 1main)` and `(33, 2supplemental)` rows and their serialized identities are unchanged. Future reviewed operations may use another numbered key, but a different key never permits reimporting existing rows or bypassing a changed source. Do not change a started selection or delete checkpoints to recover from a mismatch. Retain the frozen DNN backup and reconcile all selected tables before any cutover decision. Prevent concurrent destination writes during rehearsal; identity/ownership checks alone do not prove provenance.

Isolated server/client builds and disposable SQL tests exercised all six populated tables, dependency expansion and skipped/empty domains, scope and parent checks, collision/fresh-destination protection, rollback/resume/idempotency, preserved main/turkey progress, and mapped-value reconciliation. Tests invoked the internal writer with simulated snapshot identities, not actual Azure fingerprinting, browser/API authorization, or host deployment. Existing dependency/analyzer warnings remain. Rehearse each actual source and the full host/UI workflow separately before any production decision.

### Supplementary turkey-recipient rehearsal

Migration `01000400_InitializeTurkeyRecipients` (package 1.0.4) creates `GIBSFBTurkeyRecipients` and adds a `RunKey` to the existing `GIBSFBClientsImportCheckpoint` primary key. Existing checkpoint rows receive `1main` without changing their identity or progress; the first supplementary rehearsal uses `2supplemental`. Future supplemental operations must explicitly assign a new, stable numbered key; numbers are not automatically allocated. `TurkeyID` is preserved as the identity key, `ClientID` links to the imported client, `Year` and nullable `Holiday` are copied, and nullable `CreatedOnDate` is retained as `LegacyCreatedOnDate`; Oqtane audit values are generated by the importer. A non-unique client/year/holiday index supports holiday-specific lookups and does not prevent duplicate records. Rows are scoped through the DNN client ModuleID.

For the reviewed frozen source and ModuleID 412, the user approved excluding the 96 source-wide turkey rows without an active client. Their module cannot be determined, they remain outside scoped counts, and they are reported separately as skipped. Keep the DNN backup as their archive of record; no source rows are deleted and no Oqtane archive rows are created. Preflight and the writer permit this exception only for ModuleID 412 with exactly 96 source-wide turkey orphans; other nonzero counts or other modules remain blocked. The count alone does not identify the reviewed records: the operator must confirm the same frozen source snapshot and repeat preflight before running. Destination parent validation for scoped rows remains mandatory.

Finish the existing rehearsal first, without changing its saved table selection or deleting its checkpoint. Stop all importer instances and requests before applying the upgrade; old importer code assumes one checkpoint per destination and must not run against a table containing multiple runs. Back up the destination and verify the migration and preserved main checkpoint in isolation before upgrading the rehearsal environment. The migration deliberately does not support automatic downgrade because that could discard supplementary progress. After a separately verified isolated upgrade, choose **Include turkey recipients in reports** to add turkey to read-only preflight and reconciliation without changing the saved plan. Existing tables may show expected collisions from the completed rehearsal; inspect the turkey results separately. Then use **Run/Resume Separate Turkey Rehearsal** against the same host-configured isolated destination and as the same administrator. The server requires the original completed checkpoint to match the original source, scope, ordered plan, audit identity and policy, and checks parent ownership in each transaction. The new table must initially be empty for the destination and free of source-ID collisions. Turkey batches and their separate checkpoint row commit together; do not delete either checkpoint row to bypass a mismatch. For Azure copies the frozen source must also include the turkey table and grant the read-only source account SELECT and metadata access; the supplementary fingerprint covers the original source tables as well as turkey. Original and supplementary source fingerprints are rechecked before a chunk commits, which adds scans and can extend request time.

The client and server were built to isolated output. A disposable LocalDB SQL Server test with synthetic rows exercised the migration and preserved checkpoint, prerequisite and parent/orphan checks, rollback on a simulated identity change, batch resume, collision protection, idempotent retry, null and trailing-space preservation, and key/value reconciliation. The test invoked the internal writer with a simulated source identity; it did not test the UI, authorization, Azure fingerprinting, actual-source data, host upgrade, or request interruption. This is not approval for production import. Verify the complete workflow and operational recovery in an isolated environment before use.

The writer validates destination client ownership and legacy identifiers, and linked Christmas family relationships, in the batch transaction. A fresh-destination check does not establish provenance if other processes modify the destination after the run starts; matching legacy identifiers alone are not proof of provenance. An internal bounded multi-batch invocation validates the snapshot and fields once per invocation and checks destination column shapes and identity keys between batches. Cancellation is observed before a batch starts, not during validation or a transaction; already committed batches remain durable and can be resumed. It does not guard all schema or constraint changes, or bound memory by photo size. These limitations must be addressed and tested at representative scale before use.

**A report is not approval to import.** Import remains disabled. Metadata checks block disabled/untrusted constraints, disabled unique indexes, and disabled triggers, but actual constraint enforcement, trigger side effects, run ownership, and source-data provenance still require an isolated operational rehearsal and approval. Archive duplicate handling remains unimplemented; omit merged tables from the first import. Before production enablement, rehearse on an isolated fresh destination using a consistent read-only snapshot of the actual source, verify exclusions and all mapped field values, test interruption, resume, retry and rollback, and review the results and cutover/rollback plan. Prevent concurrent destination writes during import. Repeat preflight and full mapped-value reconciliation after changes.

Christmas history uses a preservation policy, not source cleanup. Preflight classifies scoped rows once. Under the approved policy, source-wide parentless Christmas rows (20 at the last user preflight) are explicitly skipped and reported as a warning. Their module cannot be inferred, so they are excluded from scoped counts rather than assumed to belong to module 412. They remain unchanged in DNN; retain the DNN backup as their archive of record:

- Valid client and matching active family: keep the family relationship.
- Valid client and no family link or no matching family ID in either table: designate client-level history, leave `FBClientsAddFamMemId` null, and preserve the original ID in `LegacyClAddFamMemId`.
- Valid client and family ID found only in merged history: designate client-level history with the original legacy ID retained for archive review; do not recreate an active family member. An archived ID match is a review candidate, not proof of a unique archive association. Importing merged-family records is optional. If omitted, retain the DNN backup as the archive of record and perform archive review there; these archive records will not be available in Oqtane. Preflight still requires read-only access to the source merged-family ID column for classification.
- Missing client: skip the Christmas row; do not create a client or import the row into active or merged Oqtane history. Preserve it in the DNN backup and count it separately as an approved exclusion during future reconciliation. No Oqtane exception storage is required for these approved skips. The turkey exception is a separate, narrowly scoped policy above; other history orphans remain blocked.
- Active family belonging to another client: remain blocked for review; do not silently change client IDs.

All six `_Merged` tables can be omitted from the saved selection for the first import. This excludes archived client/history records from Oqtane; it does not delete them from DNN. Parentless Christmas rows are also explicitly excluded under the policy above.

The report counts scoped groups separately from unassigned source-wide orphans. Client-level and archived-family cases are warnings describing the approved mapping; the rehearsal writer applies the supported active-table transformations, while archive review remains external. It uses the saved source ModuleID, preserves original DNN IDs in legacy fields, and maps the destination ModuleId independently. All source records remain unchanged.

Add **FBClients Import Data** to a staff page restricted to administrators. In the module's **Import Data Settings**, enter a SQL Server connection string identifying the DNN server and database, then save. The field is write-only: leave it blank to keep a saved connection, enter a replacement, or check **Remove the saved connection**. The server encrypts the connection with ASP.NET Core Data Protection before storing it in a private module setting; it is not returned to the browser. Persist and share the Data Protection key ring across host instances and restarts. Losing those keys makes the saved connection unreadable. Use HTTPS and prevent request-body logging for the connection endpoint.

A host administrator can enable the bounded rehearsal only for a designated disposable FBClients destination. Set `FBClientsImport:Rehearsal:Enabled` to `true` and `FBClientsImport:Rehearsal:DestinationModuleId` to the isolated module ID (33 for this rehearsal) in server-side host configuration. The button/API remain unavailable unless both values are present; the server enforces the exact configured module and site. A request writes at most 25 logical batches of 100 rows. For frozen Azure copies, all requested batches from the current table form one transaction with one checkpoint update (up to 2,500 rows); the request stops at a table boundary. Native SQL Server snapshots retain separate batch transactions. Resume only as the same administrator with the identical saved plan and source snapshot. Remove or disable this host setting after the rehearsal. Do not configure this against a production destination.

The rehearsal UI reports elapsed request time in minutes and seconds after success or failure. This measures the full service round trip, including verification and network time, not just writes or cumulative import duration. The timer is local to the page and is not stored in the checkpoint.

### Frozen Azure SQL rehearsal source

Azure SQL Database copies are not native SQL Server snapshots. To use `inetDNN_FamilyPantry_Copy`, first disconnect applications/jobs and prevent all other writes and schema changes for the entire rehearsal and reconciliation period. Retain the copy unchanged for recovery. Configure the module source connection with a dedicated account granted SELECT and sufficient metadata visibility on the selected active tables, the client table, and the merged-family table needed for Christmas classification; do not use the database owner or an account with write/schema permissions. The importer rejects visible table write/schema permissions. This does not freeze other principals: the host's freeze confirmation is an operational responsibility, not a database-enforced read-only guarantee.

Add these values to the existing Oqtane Server rehearsal configuration (keep destination 33):

```json
"Rehearsal": {
  "Enabled": true,
  "DestinationModuleId": 33,
  "FrozenAzureDatabase": "inetDNN_FamilyPantry_Copy",
  "AzureCopyFreezeConfirmed": true
}
```

Do not set `AzureCopyFreezeConfirmed` until the copy has actually been isolated from writers. The Azure path checks engine edition, exact database name, server, database ID and creation time. It fingerprints complete selected active tables (including rows outside source ModuleID 412), client records needed for scope/orphan checks, schema metadata, and distinct merged-family IDs used for Christmas classification. SHA-256 is computed from streamed JSON, including nulls, binary data and audit values. The checkpoint binds this identity and content fingerprint. Changing any covered content/schema or replacing the database blocks resume; do not delete the checkpoint to bypass this protection.

For Azure copies, fingerprints are scanned once before request validation and once inside the chunk transaction before commit: two scans per active request regardless of whether it contains one or up to 25 logical batches (previously eleven scans for ten batches). The freshly verified request fingerprint is reused for checkpoint lookup; it is never cached across requests. The entire chunk must match that fingerprint before committing either rows or progress. Existing checkpoint identities and the fixed logical batch size of 100 remain unchanged, so existing runs resume without reset. The chunk stops at the current table boundary; a subsequent request starts the next table. Source changes after initial validation are detected after tentative writes and roll back the entire chunk, not earlier committed chunks. Cancellation between logical batches also rolls back the chunk. Larger chunks hold destination locks longer and increase transaction-log/rollback exposure; do not run concurrent destination writers. Keep destination triggers free of nontransactional external side effects. These full-source scans can still be expensive, particularly for photos and visits; allow sufficient host request timeouts. Fingerprints detect differences at scan times but cannot prevent concurrent or transient writes; keep the operational freeze in place. The native SQL Server snapshot path is unchanged when `FrozenAzureDatabase` is omitted. No real-Azure rehearsal or measured speedup is implied by successful builds.

Alternatively, a host administrator can configure approved named sources in the **Oqtane Server's** configuration. For example:

```json
{
  "FBClientsImport": {
    "Rehearsal": {
      "Enabled": false,
      "DestinationModuleId": 33
    },
    "Sources": {
      "dnnBackup": {
        "Name": "DNN backup",
        "TenantId": 1,
        "SiteId": 1,
        "ConnectionStringName": "DnnImportSource"
      }
    }
  }
}
```

Replace the tenant and site IDs with the destination's actual IDs. Provision `ConnectionStrings:DnnImportSource` through secure host configuration (for example, the server environment variable `ConnectionStrings__DnnImportSource`); do not commit credentials. The connection string must identify the DNN database explicitly. Use an account with **read-only** access to only the required source tables and sufficient SQL Server metadata visibility. The application requests read-only intent, but that is not a substitute for database permissions. Restrict TLS and certificate settings appropriately for your environment. Restart the host if necessary after changing its configuration.

The server independently requires the site Administrator role and module View permission; configuring the connection or saving the selection also requires module Edit. Choose **Module connection** or a configured named source, select **Connect and Inspect Tables**, check visible tables, then **Save Table Selection**. Replacing the module connection can point an existing selection at a different database, so inspect it again. A table reported as missing or not visible may exist without metadata permissions. Use a restored source backup when possible and protect it as sensitive client data. No destination client data is changed by this screen.

## FBClients Lists

Install or upgrade the FBClients package to **1.0.1**. The `01000100_InitializeLists` migration creates `GIBSFBClientsLists` without changing the existing FBClients tables. After the upgrade, add **FBClients Lists** to an Oqtane page from the module picker.

The migration does not copy data from the standalone Lists table. To transfer entries, import the raw JSON array in the standalone export's `Content` field. Oqtane will reject the full standalone export because it belongs to a different module definition.

Select a list to see its entries and parent. **Add New List** creates a root or child list with its first entry; names must be unique under the same parent. **Add List Entry** and **Edit** manage the entry text and value. For sortable lists, use **Move Up** and **Move Down**. Deleting an entry or a whole list also deletes its descendants; a list with the same name under a different parent is not affected.

Inject `GIBS.Module.FBClients.Lists.Services.IListsService` and call `GetListsAsync("City", ModuleState.SiteId)` to look up entries by name from another module. This returns matches ordered by sort order and then entry ID, across FBClients Lists instances on the current site where the caller has View permission. Anonymous callers can only see publicly viewable instances. The alias-relative HTTP endpoint is `GET /api/FBClientsLists/by-name?siteid=123&listName=City`; URL-encode the list name.

### Address dropdowns

The client form's **Town** dropdown uses the root list named `City`. In the same FBClients Lists instance, add village entries to a child list under the corresponding city entry. Selecting a town loads its direct children by parent entry ID, regardless of the child list name. Give users who edit client addresses **View** permission on that instance.

The form displays **Entry Text** and saves **Entry Value**. Use place names for values (up to 50 characters for a city and 20 for a village). Use unique city values across the site's visible lists and unique village values under each city. To match existing addresses, use their saved spellings for the values. The previous hardcoded options are no longer used; saved addresses remain visible if their values are missing from the configured lists, and **OTHER** still allows free-text village entry.

### Client form option lists

Create these named lists in **FBClients Lists** and grant client editors **View** permission on the owning module:

| List name | Form field |
| --- | --- |
| `ClientEthnicity` | Client and household member ethnicity |
| `ClientDisability` | Client disability checkboxes |
| `ClientIncome` | Primary income source and monthly income source |
| `ClientExpense` | Monthly expense category |
| `ClientTrueFalseQuestions` | Client true/false questions |
| `ClientAFMRelationship` | Household member relationship to client |
| `ClientLanguage` | Preferred language |
| `ClientSuffix` | Client and household member suffix |
| `ClientServiceLocation` | Client Origination and Visit Service Location |

Use suffix values of up to 10 characters for clients or 15 for household members, and location values of up to 30 characters for Client Origination or 50 for visits. New clients default to the first eligible configured location; new visits inherit the client’s location or use the first configured visit location if the client has none. If no eligible location is configured, the field stays blank.

The form displays **Entry Text** and saves **Entry Value**. Use values of up to 100 characters for ethnicity, monthly income, and expense. Only income values of up to 50 characters appear in **Primary Income Source**. Question values can be up to 30 characters and relationship values up to 25. For languages, use short codes such as `en` or `es` (up to 2 characters); a new client defaults to `en` only if that option is configured. Disability values cannot contain commas, and the saved selections share a 250-character limit. Existing selections remain visible while editing even if they are no longer in the lists. Keep question values stable when changing labels so existing answers stay linked.

## Topaz visit signatures

The **Client Visits** tab on the Edit page supports optional Topaz SigWeb signature capture, using the same 500 x 100 PNG format as the legacy DNN module.

### Workstation setup

- Connect a compatible Topaz signature pad and install the Topaz SigWeb software on each signing workstation. The SigWeb service must be running; installing it on the web server is not sufficient.
- The browser loads Topaz's SDK from `https://www.sigplusweb.com/SigWebTablet.js` on the first signing attempt. Internet access and a content security policy that permits that script are required. No client or visit data is sent to that site by the module.
- Allow browser access to the local SigWeb service, including local-network access if prompted. For HTTPS sites the SDK uses `https://tablet.sigwebtablet.com:47290`; for HTTP sites it uses port 47289. Any content security policy must allow the corresponding service connection. Keep SigWeb and its local HTTPS certificate current.

### Recording a signature

1. Open a saved client and select **Client Visits**. Enter the visit details or edit an existing visit.
2. Click **Sign** beside **Save/Update Visit**. The signature area appears above Notes. Sign on the pad; **Clear** lets the client start again.
3. Click **Save/Update Visit** to complete signature capture and save the visit in one step; there is no separate Done button. Blank signatures and capture failures display an error and do not save the visit. Click **Sign** to retry capture after an error.
4. **Cancel** discards the in-progress replacement and retains the previous signature. **Remove Signature** removes the preview; save the visit to persist removal.

Unsigned visits remain supported.

For deployment verification with a connected pad, test signing, clearing, cancelling a replacement, removing a signature, reloading saved visits, repeated pallet visits, locked accounts, and a workstation without SigWeb. Hardware capture must be verified on an actual signing workstation.

## FBClients Inventory

Install a build of the FBClients package that includes the Inventory management screens. The **1.0.2** migration `01000200_InitializeInventory` creates `GIBSFBProductCategory`, `GIBSFBProducts`, `GIBSFBSuppliers`, `GIBSFBInvoices`, and `GIBSFBLineItems` in the Oqtane tenant database. It reuses the existing `GIBSFBClientsVisitItem` table. The management screens do not require another schema migration.

Add **FBClients Inventory** to a staff-only page. Grant staff **View** permission to browse and run reports, and **Edit** permission to manage the catalog and invoices. Note the **Inventory Module ID** shown on the dashboard for the Ordering settings.

- **Product Categories, Products, and Suppliers:** Manage the catalog, ordering instructions and limits, and supplier contact information. Mark unused catalog entries inactive instead of deleting them; existing invoices and reports can still show them.
- **Invoices:** Search by invoice number, supplier, or organization. Save an invoice header, then add its line items. Selecting a product fills in its current count, price, and weight per case; those values are saved on the line. Deleting an invoice also deletes its line items after confirmation.
- **Food Inventory Report:** Choose an inclusive invoice-date range and a grouping. The report shows subtotals and grand totals for cases, weight, and cost for this Inventory instance only. Reports over 10,000 line items require a shorter date range.

Optional **FBClients Lists** named `ClientOrganizations` and `InventoryReportingType` provide suggestions for invoice organization and line-item report type. Staff can also enter values directly. This module does not import DNN rows, manage legacy translations, or maintain on-hand balances or stock decrements.

When importing data, use the new Oqtane inventory module ID for `ModuleId` and retain the source IDs in the `Legacy...Id` columns. The separate importer above preserves primary IDs and verifies category, supplier, invoice, visit, and product references before copying those IDs; it blocks collisions instead of inventing a remapping. Use `NULL` for an uncategorized product instead of the DNN default category ID `0`, which cannot reference a valid category. Historical order items and invoice line items intentionally retain the legacy design with no product foreign key, so the importer enforces product identity and ownership transactionally. Do not rewrite existing order history without a verified mapping.

## FBClients Ordering

1. Add **FBClients Ordering** to a dedicated HTTPS page on the same Oqtane site as FBClients and Inventory. Give **All Users** View permission on the ordering page and module for SMS recipients who are not signed in; keep the staff FBClients and Inventory modules private.
2. In Ordering **Settings**, enter the **Inventory Module ID** and note the **Ordering Module ID**. Ordering uses the selected inventory's active products and active categories. Category `SortOrder` and `OrderingInstructions` control the display. Product `Limit` sets the default quantity, and `LimitQuantities` retains the DNN syntax `4=3,5=4` for exact household sizes. Missing or zero limits hide a product unless an applicable positive override exists; quantities are capped at 100 per product. The former JSON catalog setting is no longer used.
3. In the staff FBClients **Settings**, enter the **Ordering Module ID** and set **Client Order Page** to the full HTTPS page URL (for example `https://your-site.example/food-order`, without `?key=`). Configure Twilio as described below.

Clients open their SMS link, see the visit date, name, bag allowance, household size, and category instructions, and select at least one product. The server validates current product limits and saves the items and **Submitted** status in one transaction. An order does not decrement stock. Refreshing or reusing a submitted link cannot create another order. The public page does not display internal visit notes or load the legacy Google Translate widget.

Native links expire after 24 hours and work only for their intended tenant, site, Ordering instance, Inventory instance, and visit. Links generated by the earlier JSON-catalog implementation are not compatible. Treat links like passwords: do not forward them or include them in analytics. Configure the ordering page with a no-referrer policy, no third-party scripts, and redacted URL query logging at the host and proxy. Keep the site's ASP.NET Core Data Protection keys persistent and shared across instances serving the same site so that restarts or load balancing do not invalidate active links.

## FBClients Fulfillment

1. Add **FBClients Fulfillment** to a staff-only page. In its settings, enter the **FBClients Module ID** for the visit queue and the **Inventory Module ID** used by Ordering. Both modules must be on the same site. Optionally select a default service location, show categories, or use a two-column packing layout.
2. Grant staff **View** on Fulfillment and the linked FBClients module. Without **View** on the linked Inventory (or if Inventory is not configured), orders still open and show saved product IDs and quantities, but not product names. Paper entry and marking an order filled require Inventory **View**. Grant **Edit** on Fulfillment for paper entry, packing, and pickup texts. The queue links to a client's Visits tab when the worker also has access to edit that client page and module.
3. Filter the queue by visit date, location, or order status. **Enter Order** saves a paper order and moves it to **Ready to Fill**, the same status as an online submission. **Process Order** shows the packing checklist, bag allowance, household size, and staff visit notes. Check every item before selecting **Mark Order Filled**. The checkboxes are not saved as packing progress; only the completed status is saved.
4. Optionally enable **Pickup Texts** in Fulfillment Settings. The server reuses the linked FBClients module's Twilio credentials; no credentials are copied to Fulfillment. Confirm **Pickup Text** on an **Order Filled** visit for an unlocked client with a valid saved cell number. The visit changes to **Pickup Text Sent** only after Twilio accepts the message. Acceptance is not delivery confirmation. If the result is uncertain, check the Twilio console before retrying; there are no automatic retries.

Fulfillment reuses the existing visits, order items, and Inventory catalog. No additional schema migration is required. It does not import historical DNN data or decrement stock.

## Food-order texts (Twilio)

In module **Settings**, enable **Show Send Text** and configure **Food Bank Name**, **Twilio AccountSid**, **Twilio AuthToken**, **Twilio Phone Number**, and **Client Order Page**. Re-save existing settings to mark the SID and token private using Oqtane's settings mechanism. The auth token is masked in the editor. Do not put credentials in the order URL or source code.

Use a positive **Ordering Module ID** for the native Oqtane page. Leave it at `0` only to retain an existing external handler: FBClients appends the numeric Oqtane `VisitId-ClientId` to the configured HTTPS prefix (for example `https://your-site.example/food-order?key=`). That handler must validate access independently. The native page rejects these numeric links.

Save the client's phone number and set **Phone Type** to **Cell**. US/Canadian 10-digit numbers are normalized to `+1`; international numbers must include `+` and the country code. Configure an SMS-capable Twilio sender; trial-account recipient restrictions, registration, consent and messaging policies still apply.

On **Client Visits**, each saved visit for today with standard status and a saved cell number has **Send Text**. Locked accounts cannot send. Twilio is called only on the server; the browser sends only visit/module identifiers. The server independently checks edit permission, visit ownership, settings and eligibility.

After Twilio accepts the message, only the visit's order status is changed to **Link Sent**. Acceptance is not delivery confirmation; check the Twilio console for delivery failures. Visits already marked Link Sent or further along cannot be resent using this action. The button is disabled during another visit/signature operation and after an accepted or unconfirmed send in the current page.

If Twilio acceptance is uncertain (for example, a timeout), check the Twilio console before reloading and retrying. There are no automatic retries or delivery webhooks. If Twilio accepts the text but saving the visit status fails, the page displays the message SID and a warning not to resend. Do not use concurrent send requests from multiple tabs/operators for the same visit; this flow is not a distributed idempotency service.

The Server project references Twilio 8.1.0 and Newtonsoft.Json 13.0.4. Debug deployments and the release package include their runtime DLLs. System.IdentityModel.Tokens.Jwt is pinned to 8.19.2 to align Twilio's transitive IdentityModel dependencies with the existing Oqtane host; those host-supplied DLLs are not overwritten by the package. No Twilio package is added to the Client project.

Deployment verification: use an authorized test recipient. Verify **Link Sent** after delivery; open the link while signed out; check household limits; submit and verify the visit shows **Submitted** and each selected product is stored once. Try submitting from two tabs, opening the same link after submission, using an altered or expired link, opening another Ordering instance with that link, and editing a visit from a staff tab opened before submission. For SMS, also check missing settings, a non-cell/invalid number, a prior-day visit, an already-sent visit, and a locked account.
