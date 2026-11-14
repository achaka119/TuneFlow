# Optional end-to-end suite

Requires Node.js and a running TuneFlow instance. From this directory:

```sh
npm install
npx playwright install chromium
npm test
```

Set BASE_URL if not using http://localhost:5080. This suite was supplied but could not run in the restricted build environment. A connected browser was used for the documented manual workflow instead. The browser suite covers validation, player start/pause, search filtering, persistence, favorite state, playlist editing/deletion and mobile controls. Browser dependencies are not needed to run TuneFlow itself.
