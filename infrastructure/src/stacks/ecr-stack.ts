import { devConfig } from "../config/dev";
import { qaConfig } from "../config/qa";
import { prodConfig } from "../config/prod";
import { Stack, StackProps, RemovalPolicy } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { createSimpleEcrRepository } from '../helpers'
import { Repository } from "aws-cdk-lib/aws-ecr";


const env = process.env.ENV ?? 'dev';
const config = env === 'ai-prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;

export class PlatformEcrStack extends Stack {
    public transformerapiEcrRepository: Repository;

    constructor(scope: Construct, id: string, props?: StackProps) {
      super(scope, id, props);

      const prefix = `${config.feAppName}`;
      
      // Create the transformer.api ECR
      this.transformerapiEcrRepository = createSimpleEcrRepository(this, `${prefix}-ue1-ecr-tranformer-api`);

    }
}