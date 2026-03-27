import { config } from "@dotenvx/dotenvx/src/lib/main";
import { AppConfig } from "../types/config";
import * as sharedConfig from "./shared";
import { PrivateRDSConstructProps } from "infrastructure-templates";
import { IVpc, SubnetType, InstanceClass, InstanceSize } from 'aws-cdk-lib/aws-ec2';

/* 
 Common for all stacks in dev environment
*/

const env = "dev";
const awsAccountNumber = "563171196787";
const vpcId = "vpc-0b68223f4b190c630";
const rootDomain = "dev.transflo.com";
const hostedZoneId = "Z0044227EV3URHJVUB8W";
const indexFile = "index.html";
const ecsClusterName = "Dev-Cluster";
const ecsClusterArn = "arn:aws:ecs:us-east-1:563171196787:cluster/Dev-Cluster";


export const devConfig: AppConfig = {
  name: sharedConfig.name,
  description: sharedConfig.description,
  region: sharedConfig.region,
  awsAccountNumber,
  vpcId,
  vpcSubnets: { subnetType: SubnetType.PRIVATE_WITH_EGRESS },
  albSubnetIds: [
    "subnet-0a3c3fc76cdd11307",
    "subnet-0c42b0154d0ea1df3",
  ],
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
