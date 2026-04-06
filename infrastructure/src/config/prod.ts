import { config } from "@dotenvx/dotenvx/src/lib/main";
import { AppConfig } from "../types/config";
import * as sharedConfig from "./shared";
import { PrivateRDSConstructProps } from "infrastructure-templates";
import { RemovalPolicy } from "aws-cdk-lib";
import { IVpc, SubnetType, InstanceClass, InstanceSize } from 'aws-cdk-lib/aws-ec2';
/* 
 Common for all stacks in prod environment
*/

const env = "prod";
const awsAccountNumber = "234750853127";
const vpcId = "vpc-08f15f69a2b3afa1d";
const rootDomain = "transflo.com";
const hostedZoneId = "Z0660679PYYQWQZW7BOS";
const indexFile = "index.html";
const ecsClusterName = "Prod-Cluster";
const ecsClusterArn = "arn:aws:ecs:us-east-1:234750853127:cluster/Prod-Cluster";


export const prodConfig: AppConfig = {
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
  aspnetcoreEnv: 'Production',
  ecsClusterName,
  ecsClusterArn,
  rootDomain,
  hostedZoneId,
  indexFile,
  platformUIStackName: sharedConfig.plaformUIStackName,
  ecrStackName: sharedConfig.ecrStackName,
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