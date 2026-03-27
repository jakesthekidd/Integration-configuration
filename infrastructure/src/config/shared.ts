import { SubnetType, InstanceClass, InstanceSize, InstanceType } from "aws-cdk-lib/aws-ec2";
import { DatabaseInstanceEngine, PostgresEngineVersion, LicenseModel } from "aws-cdk-lib/aws-rds";
import { RemovalPolicy } from "aws-cdk-lib";

/**
 * Common
 */
export const name = "platform";
export const description = "The infrastructure stack for the Platform team";
export const region = "us-east-1";
export const vpcSubnets = SubnetType.PRIVATE_WITH_EGRESS;
export const apiSubnetType = SubnetType.PRIVATE_WITH_EGRESS;
export const albSubnetType = SubnetType.PRIVATE_WITH_EGRESS;
export const feAppName = "platform";
export const plaformUIStackName = "PlaformUiStack";
export const ecrStackName = "PlaformEcrStack";
export const transformerapiStackName = "transformerapiStack";
export const transformerapiSubDomain = 'api.transformer.platform';
export const postgresStackName = 'PlaformPostgreSQLStack';
export const vpnCidr = "10.36.88.3/32"; // CIDR block for VPN connectivity


/**
 * Postgres RDS
 */
export const dbEngine = DatabaseInstanceEngine.postgres({
  version: PostgresEngineVersion.of("17.6", "17"),
});
export const dbPort = 5432;
export const dbRootUsername = "postgres";
export const dbBackupRetentionDays = 7;
export const dbDeleteAutomatedBackupsOnDestroy = false;
export const dbEncryptStorage = true;
export const dbAllocatedStorageGiB = 50;
export const postgreSQLAvailabilityZone = "us-east-1a";