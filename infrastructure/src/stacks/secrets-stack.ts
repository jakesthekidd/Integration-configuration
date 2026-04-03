import { App, Stack, StackProps } from "aws-cdk-lib";
import { Secret } from "aws-cdk-lib/aws-secretsmanager";
import { RemovalPolicy } from "aws-cdk-lib";
import { devConfig } from "../config/dev";
import { qaConfig } from "../config/qa";
import { prodConfig } from "../config/prod";

const env = process.env.ENV ?? 'dev';
const config = env === 'prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;

export class PlatformSecretsStack extends Stack {
    public readonly transformerSecret: Secret;   

    constructor(scope: App, id: string, props?: StackProps) {
        super(scope, id, props);

        // Shared secret for Transformer Platform team
        this.transformerSecret = new Secret(this, 'transformerSecret', {
            secretName: 'platform/transformer/secrets',
            description: 'Shared secret for Transformer Platform project',
            removalPolicy: RemovalPolicy.DESTROY,
        });
    }
}