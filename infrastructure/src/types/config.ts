import { IVpc, SubnetType, SubnetSelection } from "aws-cdk-lib/aws-ec2";
import { Repository } from "aws-cdk-lib/aws-ecr";
import { PrivateRDSConstructProps } from "infrastructure-templates";

export type AppConfig = {
  name: string;
  feAppName: string;
  description: string;
  env: string;
  region: string;
  apiSubnetType: SubnetType;
  albSubnetType: SubnetType;
  albSubnetIds?: string[];
  awsAccountNumber: string;
  vpcId: string;
  vpcSubnets?: SubnetSelection;
  rootDomain: string;
  hostedZoneId: string;
  ecsClusterName: string;
  ecsClusterArn: string;
  transformerapiStackName: string,
  transformerapiSubDomain: string,
  indexFile: string;
  platformUIStackName: string;
  ecrStackName: string;
  postgresStackName: string;

  postgresDBProps: (vpc: IVpc) => PrivateRDSConstructProps;

};
