import { App } from "aws-cdk-lib";
import { devConfig } from "./config/dev";
import { qaConfig } from "./config/qa";
import { prodConfig } from "./config/prod";
import { PlatformUIStack } from "./stacks/platform-ui-stack";
import { PlatformEcrStack } from "./stacks/ecr-stack";
import { PlatformPostgresStack } from "./stacks/postgres-rds-stack";
import { transformerapiStack } from "./stacks/ECS/transformer.api-stack";

// Determine environment
const env = process.env.ENV ?? "dev";
const stackToDeploy = process.env.STACK;
const config = env === "prod" ? prodConfig : env === "qa" ? qaConfig : devConfig;

const app = new App();
const cdkEnv = { account: config.awsAccountNumber, region: config.region };

const PlatformUiStack = new PlatformUIStack(app, config.platformUIStackName, { env: cdkEnv });

const ecrStack = new PlatformEcrStack(app, config.ecrStackName, { env: cdkEnv });

const postgresStack = new PlatformPostgresStack(app, config.postgresStackName, { env: cdkEnv });

const ecstransformerapiStack = new transformerapiStack (app, config.transformerapiStackName, { env: cdkEnv, ecrStack: ecrStack });
