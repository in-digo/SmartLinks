"use strict";

const fs = require("node:fs");

const [sourcePath, destinationPath] = process.argv.slice(2);

if (!sourcePath || !destinationPath) {
    throw new Error("Usage: node prepare-newman-environment.cjs <source-environment> <destination-environment>");
}

const apiKeyPath = process.env.SMARTLINKS_MANAGEMENT_API_KEY_FILE;
const managementBaseUrl = process.env.SMARTLINKS_MANAGEMENT_BASE_URL;
const redirectBaseUrl = process.env.SMARTLINKS_REDIRECT_BASE_URL;

if (!apiKeyPath)
    throw new Error("SMARTLINKS_MANAGEMENT_API_KEY_FILE is required");

if (!managementBaseUrl)
    throw new Error("SMARTLINKS_MANAGEMENT_BASE_URL is required");

if (!redirectBaseUrl)
    throw new Error("SMARTLINKS_REDIRECT_BASE_URL is required");

const managementApiKey = fs.readFileSync(apiKeyPath, "utf8").trim();

if (!managementApiKey)
    throw new Error("Management API key secret is empty");

const environment = JSON.parse(fs.readFileSync(sourcePath, "utf8"));

function setEnvironmentValue(key, value) {
    const variable = environment.values?.find(candidate => candidate.key === key);

    if (!variable)
        throw new Error(`Postman environment variable is missing: ${key}`);

    variable.value = value;
}

setEnvironmentValue("managementBaseUrl", managementBaseUrl.replace(/\/+$/, ""));
setEnvironmentValue("redirectBaseUrl", redirectBaseUrl.replace(/\/+$/, ""));
setEnvironmentValue("managementApiKey", managementApiKey);

fs.writeFileSync(
    destinationPath,
    `${JSON.stringify(environment, null, 2)}\n`,
    {
        encoding: "utf8",
        flag: "w",
        mode: 0o600
    });

fs.chmodSync(destinationPath, 0o600);
