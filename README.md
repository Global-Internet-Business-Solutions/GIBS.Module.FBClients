# GIBS.Module.FBClients

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

## Food-order texts (Twilio)

In module **Settings**, enable **Show Send Text** and configure **Food Bank Name**, **Twilio AccountSid**, **Twilio AuthToken**, **Twilio Phone Number**, and **Client Order Page**. Re-save existing settings to mark the SID and token private using Oqtane's settings mechanism. The auth token is masked in the editor. Do not put credentials in the order URL or source code.

**Client Order Page** is the URL prefix already used by the DNN module. The module appends the current Oqtane `VisitId-ClientId` key, not legacy DNN IDs. For example, `https://your-site.example/food-order?key=` becomes `https://your-site.example/food-order?key=123-456`. Include the actual query parameter or trailing path separator expected by your ordering page. An absolute HTTPS URL is required. The receiving page must recognize these Oqtane IDs and validate access; the numeric key alone is not proof of authorization. This feature sends links and does not implement the receiving ordering page.

Save the client's phone number and set **Phone Type** to **Cell**. US/Canadian 10-digit numbers are normalized to `+1`; international numbers must include `+` and the country code. Configure an SMS-capable Twilio sender; trial-account recipient restrictions, registration, consent and messaging policies still apply.

On **Client Visits**, each saved visit for today with standard status and a saved cell number has **Send Text**. Clicking it sends `<Food Bank Name> Ordering: <configured prefix><VisitId>-<ClientId>`. Locked accounts cannot send. Twilio is called only on the server; the browser sends only visit/module identifiers. The server independently checks edit permission, visit ownership, settings and eligibility.

After Twilio accepts the message, only the visit's order status is changed to **Link Sent**. Acceptance is not delivery confirmation; check the Twilio console for delivery failures. Visits already marked Link Sent or further along cannot be resent using this action. The button is disabled during another visit/signature operation and after an accepted or unconfirmed send in the current page.

If Twilio acceptance is uncertain (for example, a timeout), check the Twilio console before reloading and retrying. There are no automatic retries or delivery webhooks. If Twilio accepts the text but saving the visit status fails, the page displays the message SID and a warning not to resend. Do not use concurrent send requests from multiple tabs/operators for the same visit; this flow is not a distributed idempotency service.

The Server project references Twilio 8.1.0 and Newtonsoft.Json 13.0.4. Debug deployments and the release package include their runtime DLLs. System.IdentityModel.Tokens.Jwt is pinned to 8.19.2 to align Twilio's transitive IdentityModel dependencies with the existing Oqtane host; those host-supplied DLLs are not overwritten by the package. No Twilio package is added to the Client project.

Deployment verification: use an authorized test recipient to verify the configured order link, delivery, Link Sent persistence after reload, and the receiving page's client/visit lookup. Also test missing settings, a non-cell/invalid number, a prior-day visit, an already-sent visit, and a locked account. Automated verification uses a fake sender and never sends real SMS.
