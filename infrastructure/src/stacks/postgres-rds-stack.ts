import { Stack, StackProps } from "aws-cdk-lib";
import { Construct } from 'constructs';
import { devConfig } from "../config/dev";
import { qaConfig } from "../config/qa";
import { prodConfig } from "../config/prod";
import { PrivateRDSConstruct } from "infrastructure-templates";
import {  getTransfloVpc } from "../helpers";


const env = process.env.ENV ?? 'dev';
const config = env === 'prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;


export class PlatformPostgresStack extends Stack {

    public postgresDB: PrivateRDSConstruct;
  
    constructor(scope: Construct, id: string, props?: StackProps) {
      super(scope, id, props);

      // import VPC
      const vpc = getTransfloVpc(this);

      // Create RDS
      this.postgresDB = new PrivateRDSConstruct(this, {
          ...config.postgresDBProps(vpc), // Pass props from config
      });
      
    }
  }