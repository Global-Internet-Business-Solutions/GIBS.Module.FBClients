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
