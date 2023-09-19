#!/usr/bin/bash

# download spec
wget -O ./data/spec.json https://raw.githubusercontent.com/adraffy/ens-normalize.js/main/derive/output/spec.json
wget -O ./data/nf.json https://raw.githubusercontent.com/adraffy/ens-normalize.js/main/derive/output/nf.json

# download tests
wget -O ../Tests/data/tests.json https://raw.githubusercontent.com/adraffy/ens-normalize.js/main/validate/tests.json
wget -O ../Tests/data/nf-tests.json https://raw.githubusercontent.com/adraffy/ens-normalize.js/main/derive/output/nf-tests.json
