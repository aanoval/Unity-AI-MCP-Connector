#!/usr/bin/env node

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const PACKAGE_ID = 'com.alday.unity-ai-game-maker';
const LEGACY_PACKAGE_IDS = ['com.alday.unity-ai-connector'];
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
  unity-ai <projectPath> unity-batch <jsonFile> [--unity <path>]
  unity-ai <projectPath> capture-scenes [--output <dir>] [--filter menu|buildSettings|gameplay|all] [--width <n>] [--height <n>] [--source camera|gameView] [--unity <path>]
  unity-ai <projectPath> sample-runner3d [--unity <path>]
  unity-ai <projectPath> health
  unity-ai <projectPath> tools
  unity-ai <projectPath> call <tool> <jsonArgs>

Examples:
  unity-ai /path/to/project install
  unity-ai /path/to/project config
  unity-ai /path/to/project doctor
  unity-ai /path/to/project unity-batch ./examples/batch.menu-screenshots.json
  unity-ai /path/to/project capture-scenes --output ../menu-screenshots --filter menu --width 1080 --height 1920
  unity-ai /path/to/project sample-runner3d --unity /Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity
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
  const configPath = path.join(projectPath, 'UserSettings', 'UnityAiGameMaker.json');
  if (!fs.existsSync(configPath)) {
    throw new Error(`Config not found: ${configPath}. Start Unity AI Game Maker once from Unity first.`);
  }

  return readJson(configPath);
}

function readConfigIfExists(projectPath) {
  const configPath = path.join(projectPath, 'UserSettings', 'UnityAiGameMaker.json');
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

function findUnityExecutable(rest) {
  const requested = parseFlagValue(rest, '--unity') ?? process.env.UNITY_EXECUTABLE;
  if (requested) {
    if (!fs.existsSync(requested)) {
      throw new Error(`Unity executable not found: ${requested}`);
    }
    return requested;
  }

  const candidates = [
    '/Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity',
    '/Applications/Unity/Unity-6000.5.1f1/Unity.app/Contents/MacOS/Unity',
    '/Applications/Unity/Unity.app/Contents/MacOS/Unity',
  ];

  const found = candidates.find((candidate) => fs.existsSync(candidate));
  if (!found) {
    throw new Error('Unity executable not found. Pass --unity <path> or set UNITY_EXECUTABLE.');
  }

  return found;
}

function runUnityBatch(projectPath, commands, rest = []) {
  const expandedCommands = expandBatchMacros(commands);
  const phases = splitCompilePhases(expandedCommands);
  if (phases.length === 1) {
    return runUnityBatchPhase(projectPath, phases[0], rest);
  }

  const results = phases.map((phaseCommands, index) => ({
    phase: index + 1,
    compileBoundaryAfter: index < phases.length - 1,
    output: runUnityBatchPhase(projectPath, phaseCommands, rest),
  }));

  return {
    ok: true,
    phased: true,
    phaseCount: phases.length,
    results,
  };
}

function runUnityBatchPhase(projectPath, commands, rest = []) {
  requireUnityProject(projectPath);
  const unity = findUnityExecutable(rest);
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-ai-batch-'));
  const batchFile = path.join(tempRoot, 'commands.json');
  const outputFile = path.join(tempRoot, 'result.json');

  writeJson(batchFile, { commands });

  const result = spawnSync(unity, [
    '-batchmode',
    '-quit',
    '-projectPath',
    projectPath,
    '-executeMethod',
    'Alday.UnityAiGameMaker.Editor.UnityAiGameMakerBatch.RunFromEnvironment',
    '-logFile',
    '-'
  ], {
    encoding: 'utf8',
    env: {
      ...process.env,
      UNITY_AI_GAME_MAKER_BATCH_FILE: batchFile,
      UNITY_AI_GAME_MAKER_BATCH_OUT: outputFile,
      UNITY_AI_CONNECTOR_BATCH_FILE: batchFile,
      UNITY_AI_CONNECTOR_BATCH_OUT: outputFile,
    },
    maxBuffer: 1024 * 1024 * 20,
  });

  const output = fs.existsSync(outputFile) ? readJson(outputFile) : null;
  if (result.status !== 0) {
    const tail = `${result.stdout ?? ''}\n${result.stderr ?? ''}`.split('\n').slice(-80).join('\n');
    throw new Error(`Unity batch failed with exit code ${result.status}.\n${tail}`);
  }

  if (!output?.ok) {
    throw new Error(`Unity batch command failed: ${JSON.stringify(output, null, 2)}`);
  }

  return output;
}

function expandBatchMacros(commands) {
  return commands.flatMap((command) => {
    if (command?.tool === 'screenshots.captureMenus') {
      return [{
        tool: 'screenshots.captureScenes',
        args: {
          filter: 'menu',
          width: command.args?.width ?? 1080,
          height: command.args?.height ?? 1920,
          source: command.args?.source ?? 'camera',
          outputDir: command.args?.outputDir ?? command.args?.output,
          namePattern: command.args?.namePattern,
          cameraPath: command.args?.cameraPath,
        },
      }];
    }

    if (command?.tool !== 'script.createAndAttach') {
      return [command];
    }

    const args = command.args ?? {};
    const className = args.className;
    if (!className) {
      throw new Error('script.createAndAttach requires args.className.');
    }

    const target = {};
    if (args.targetPath) {
      target.path = args.targetPath;
    }
    if (args.targetName) {
      target.name = args.targetName;
    }
    if (!target.path && !target.name) {
      throw new Error('script.createAndAttach requires args.targetPath or args.targetName.');
    }

    const expanded = [
      {
        tool: 'script.create',
        args: {
          className,
          path: args.path,
          template: args.template,
          content: args.content,
        },
      },
      {
        tool: 'component.add',
        args: {
          ...target,
          type: className,
          reuseExisting: args.reuseExisting ?? true,
        },
      },
    ];

    const properties = args.properties ?? args.fields ?? {};
    for (const [property, value] of Object.entries(properties)) {
      expanded.push({
        tool: 'component.setProperty',
        args: {
          ...target,
          type: className,
          property,
          value,
        },
      });
    }

    return expanded;
  });
}

function splitCompilePhases(commands) {
  const phases = [];
  let current = [];
  let activeScenePath = null;

  for (const command of commands) {
    if (command?.tool === 'scene.open' && command.args?.path) {
      activeScenePath = command.args.path;
    }

    current.push(command);
    if (requiresCompileBoundary(command)) {
      if (activeScenePath && !endsWithSceneSave(current)) {
        current.push({ tool: 'scene.save', args: {} });
      }
      phases.push(current);
      current = [];
      if (activeScenePath) {
        current.push({ tool: 'scene.open', args: { path: activeScenePath } });
      }
    }
  }

  if (current.length > 0) {
    phases.push(current);
  }

  return phases;
}

function endsWithSceneSave(commands) {
  const last = commands.at(-1);
  return last?.tool === 'scene.save' || last?.tool === 'scene.saveAs';
}

function requiresCompileBoundary(command) {
  return command?.tool === 'script.create' || command?.tool === 'sample.runner3D.createScripts';
}

function readBatchFile(jsonFile) {
  const payload = readJson(path.resolve(jsonFile));
  if (Array.isArray(payload)) {
    return payload;
  }
  if (Array.isArray(payload.commands)) {
    return payload.commands;
  }
  throw new Error('Batch JSON must be an array or an object with a commands array.');
}

function captureScenes(projectPath, rest) {
  const outputDir = parseFlagValue(rest, '--output');
  const filter = parseFlagValue(rest, '--filter') ?? 'menu';
  const width = Number.parseInt(parseFlagValue(rest, '--width') ?? '1080', 10);
  const height = Number.parseInt(parseFlagValue(rest, '--height') ?? '1920', 10);
  const source = parseFlagValue(rest, '--source') ?? 'camera';

  return runUnityBatch(projectPath, [
    {
      tool: 'screenshots.captureScenes',
      args: {
        ...(outputDir ? { outputDir } : {}),
        filter,
        width,
        height,
        source,
      },
    },
  ], rest);
}

function createRunner3DSample(projectPath, rest) {
  const install = installPackage(projectPath, rest);
  const scripts = runUnityBatch(projectPath, [
    { tool: 'sample.runner3D.createScripts', args: {} }
  ], rest);
  const content = runUnityBatch(projectPath, [
    { tool: 'sample.runner3D.createContent', args: {} }
  ], rest);

  return {
    ok: true,
    install,
    scripts,
    content,
    projectPath,
    nextStep: 'Open the Unity project and play Assets/UnityAiGameMakerSample/Scenes/MainMenu.unity.'
  };
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
  const removedLegacyPackageIds = [];
  for (const legacyPackageId of LEGACY_PACKAGE_IDS) {
    if (manifest.dependencies[legacyPackageId]) {
      delete manifest.dependencies[legacyPackageId];
      removedLegacyPackageIds.push(legacyPackageId);
    }
  }

  manifest.dependencies[PACKAGE_ID] = dependencyValue;
  writeJson(manifestPath, manifest);

  return {
    ok: true,
    manifestPath,
    packageId: PACKAGE_ID,
    previousValue: previousValue ?? null,
    removedLegacyPackageIds,
    dependencyValue,
    nextStep: 'Open the Unity project. The local server auto-starts by default, or use batch mode via unity-batch / capture-scenes.'
  };
}

function printConfig(projectPath) {
  const config = readConfig(projectPath);
  return {
    ok: true,
    configPath: path.join(projectPath, 'UserSettings', 'UnityAiGameMaker.json'),
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
      : 'Open Unity (server auto-starts by default) or use: node cli/unity-ai.js <projectPath> unity-batch <jsonFile>'
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

  if (command === 'unity-batch') {
    const [jsonFile] = rest;
    if (!jsonFile) {
      throw new Error('Missing batch JSON file.');
    }
    console.log(JSON.stringify(runUnityBatch(projectPath, readBatchFile(jsonFile), rest.slice(1)), null, 2));
    return;
  }

  if (command === 'capture-scenes') {
    console.log(JSON.stringify(captureScenes(projectPath, rest), null, 2));
    return;
  }

  if (command === 'sample-runner3d') {
    console.log(JSON.stringify(createRunner3DSample(projectPath, rest), null, 2));
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
