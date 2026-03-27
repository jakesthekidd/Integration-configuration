import { devConfig } from "../config/dev";
import { qaConfig } from "../config/qa";
import { prodConfig } from "../config/prod";
import { Stack, StackProps, SecretValue } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { CfS3Construct } from "infrastructure-templates";


const env = process.env.ENV ?? 'dev';
const config = env === 'prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;


export class PlatformUIStack extends Stack {

    public frontendapp: CfS3Construct;
  
    constructor(scope: Construct, id: string, props?: StackProps) {
      super(scope, id, props);
      

      // CF + S3
      this.frontendapp = new CfS3Construct(this, {
        name: `transformer-${config.feAppName}-app-${env}`,
        useDefaultHttpSecurityHeaders: false,
        dnsConfig: {
            domain: `transformer.${config.feAppName}.${config.rootDomain}`,
            hostedZoneId: config.hostedZoneId,
            hostedZoneName: config.rootDomain,
        },
        indexFile: config.indexFile,
        // telling CF to redirect to the root if S3 responds with either of these errors so the app will handle routing
        errorResponses: [
            {
                httpStatus: 403, // forbidden
                responseHttpStatus: 200,
                responsePagePath: '/index.html',
            },
            {
                httpStatus: 404, // not found
                responseHttpStatus: 200,
                responsePagePath: '/index.html',
            }
        ]
    })
      
    }
  }