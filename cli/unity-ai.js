#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const PACKAGE_ID = 'com.alday.unity-ai-connector';
const DEFAULT_PORT = 6421;

function repoRoot() {
  return path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
}

function packagePath() {
  return path.join(repoRoot(), 'packages', PACKAGE_ID);
}

function usage() {
  console.log(`Usage:
  unity-ai <projectPath> install [--package-path <path>]
  unity-ai <projectPath> config
  unity-ai <projectPath> doctor
  unity-ai <projectPath> health
  unity-ai <projectPath> tools
  unity-ai <projectPath> call <tool> <jsonArgs>

Examples:
  unity-ai /path/to/project install
  unity-ai /path/to/project config
  unity-ai /path/to/project doctor
  unity-ai /path/to/project health
  unity-ai /path/to/project tools
  unity-ai /path/to/project call scene.listOpen '{}'
`);
}

function requireUnityProject(projectPath) {
  const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Unity manifest not found: ${manifestPath}`);
  }

  return manifestPath;
}

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function writeJson(filePath, value) {
  fs.writeFileSync(filePath, `${JSON.stringify(value, null, 2)}\n`);
}

function readConfig(projectPath) {
  const configPath = path.join(projectPath, 'UserSettings', 'UnityAiConnector.json');
  if (!fs.existsSync(configPath)) {
    throw new Error(`Config not found: ${configPath}. Start the connector once from Unity first.`);
  }

  return readJson(configPath);
}

function readConfigIfExists(projectPath) {
  const configPath = path.join(projectPath, 'UserSettings', 'UnityAiConnector.json');
  return fs.existsSync(configPath) ? readJson(configPath) : null;
}

function parseFlagValue(args, flagName) {
  const index = args.indexOf(flagName);
  if (index < 0) {
    return null;
  }

  if (index + 1 >= args.length) {
    throw new Error(`Missing value for ${flagName}.`);
  }

  return args[index + 1];
}

function installPackage(projectPath, rest) {
  const manifestPath = requireUnityProject(projectPath);
  const selectedPackagePath = path.resolve(parseFlagValue(rest, '--package-path') ?? packagePath());

  if (!fs.existsSync(path.join(selectedPackagePath, 'package.json'))) {
    throw new Error(`Package not found: ${selectedPackagePath}`);
  }

  const manifest = readJson(manifestPath);
  manifest.dependencies ??= {};

  const dependencyValue = `file:${selectedPackagePath}`;
  const previousValue = manifest.dependencies[PACKAGE_ID];
  manifest.dependencies[PACKAGE_ID] = dependencyValue;
  writeJson(manifestPath, manifest);

  return {
    ok: true,
    manifestPath,
    packageId: PACKAGE_ID,
    previousValue: previousValue ?? null,
    dependencyValue,
    nextStep: 'Open the Unity project and start Tools > Unity AI Connector > Start Local Server.'
  };
}

function printConfig(projectPath) {
  const config = readConfig(projectPath);
  return {
    ok: true,
    configPath: path.join(projectPath, 'UserSettings', 'UnityAiConnector.json'),
    bindHost: config.bindHost ?? '127.0.0.1',
    port: config.port ?? DEFAULT_PORT,
    authRequired: config.authRequired !== false,
    hasToken: Boolean(config.token)
  };
}

async function doctor(projectPath) {
  const manifestPath = requireUnityProject(projectPath);
  const manifest = readJson(manifestPath);
  const config = readConfigIfExists(projectPath);
  const installedValue = manifest.dependencies?.[PACKAGE_ID] ?? null;
  const checks = {
    unityProject: true,
    packageInstalled: Boolean(installedValue),
    configExists: Boolean(config),
    serverReachable: false
  };

  let health = null;
  if (config) {
    try {
      const host = config.bindHost ?? '127.0.0.1';
      const port = config.port ?? DEFAULT_PORT;
      health = await requestJson(`http://${host}:${port}/health`);
      checks.serverReachable = Boolean(health?.ok);
    } catch {
      checks.serverReachable = false;
    }
  }

  return {
    ok: Object.values(checks).every(Boolean),
    checks,
    installedValue,
    health,
    nextStep: checks.serverReachable
      ? null
      : 'Open Unity and start Tools > Unity AI Connector > Start Local Server.'
  };
}

async function requestJson(url, options = {}) {
  const response = await fetch(url, options);
  const text = await response.text();
  let data;

  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }

  if (!response.ok) {
    throw new Error(typeof data === 'string' ? data : JSON.stringify(data));
  }

  return data;
}

async function main() {
  const [projectPath, command, ...rest] = process.argv.slice(2);
  if (!projectPath || !command) {
    usage();
    process.exitCode = 1;
    return;
  }

  if (command === 'install') {
    console.log(JSON.stringify(installPackage(projectPath, rest), null, 2));
    return;
  }

  if (command === 'config') {
    console.log(JSON.stringify(printConfig(projectPath), null, 2));
    return;
  }

  if (command === 'doctor') {
    console.log(JSON.stringify(await doctor(projectPath), null, 2));
    return;
  }

  const config = readConfig(projectPath);
  const host = config.bindHost ?? '127.0.0.1';
  const port = config.port ?? DEFAULT_PORT;
  const baseUrl = `http://${host}:${port}`;

  if (command === 'health') {
    const data = await requestJson(`${baseUrl}/health`);
    console.log(JSON.stringify(data, null, 2));
    return;
  }

  const headers = {
    Authorization: `Bearer ${config.token}`,
  };

  if (command === 'tools') {
    const data = await requestJson(`${baseUrl}/tools`, { headers });
    console.log(JSON.stringify(data, null, 2));
    return;
  }

  if (command === 'call') {
    const [tool, jsonArgs = '{}'] = rest;
    if (!tool) {
      throw new Error('Missing tool name.');
    }

    const args = JSON.parse(jsonArgs);
    const data = await requestJson(`${baseUrl}/rpc`, {
      method: 'POST',
      headers: {
        ...headers,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ tool, args }),
    });
    console.log(JSON.stringify(data, null, 2));
    return;
  }

  usage();
  process.exitCode = 1;
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
