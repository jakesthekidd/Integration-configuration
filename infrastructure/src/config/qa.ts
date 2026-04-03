import { config } from "@dotenvx/dotenvx/src/lib/main";
import { AppConfig } from "../types/config";
import * as sharedConfig from "./shared";
import { ContainerLambdaConstructProps, PrivateRDSConstructProps, DocumentDBClusterProps, RedisClusterConstructProps } from "infrastructure-templates";
import { IVpc, SubnetType, InstanceClass, InstanceSize } from 'aws-cdk-lib/aws-ec2';

/* 
 Common for all stacks in qa environment
*/

const env = "qa";
const awsAccountNumber = "624164147597";
const vpcId = "vpc-04f7e4fc41acfc963";
const rootDomain = "qa.transflo.com";
const hostedZoneId = "Z04805792FS7DSS2SKGIO";
const indexFile = "index.html";
const ecsClusterName = "QA-Cluster";
const ecsClusterArn = "arn:aws:ecs:us-east-1:624164147597:cluster/QA-Cluster";


export const qaConfig: AppConfig = {
  name: sharedConfig.name,
  description: sharedConfig.description,
  region: sharedConfig.region,
  awsAccountNumber,
  vpcId,
  vpcSubnets: { subnetType: SubnetType.PRIVATE_WITH_EGRESS },
  apiSubnetType: sharedConfig.apiSubnetType,
  albSubnetType: sharedConfig.albSubnetType,
  feAppName: sharedConfig.feAppName,
  env,
  ecsClusterName,
  ecsClusterArn,
  rootDomain,
  hostedZoneId,
  indexFile,
  platformUIStackName: sharedConfig.plaformUIStackName,
  ecrStackName: sharedConfig.ecrStackName,
  secretsStackName: sharedConfig.secretsStackName,
  postgresStackName: sharedConfig.postgresStackName,
  transformerapiStackName: sharedConfig.transformerapiStackName,
  transformerapiSubDomain: sharedConfig.transformerapiSubDomain,


postgresDBProps: (vpc: IVpc): PrivateRDSConstructProps => {
    const sharedProps = {
      vpc,
      instanceClass: InstanceClass.T3,
      instanceSize: InstanceSize.MEDIUM,
      port: sharedConfig.dbPort,
      rootUsername: sharedConfig.dbRootUsername,
      backupRetentionDays: sharedConfig.dbBackupRetentionDays,
      deleteAutomatedBackupsOnDestroy: sharedConfig.dbDeleteAutomatedBackupsOnDestroy,
      encryptStorage: sharedConfig.dbEncryptStorage,
      allocatedStorageGiB: sharedConfig.dbAllocatedStorageGiB,
      availabiltyZone: sharedConfig.postgreSQLAvailabilityZone,
      vpn: {
            cidrs: [
              sharedConfig.vpnCidr,
            ]
      },
    }

    return {
      ...sharedProps,
      name: `${sharedConfig.name}-transformer-pg`,
      databaseEngine: sharedConfig.dbEngine,
      readReplicas: [],
    };
  },
};