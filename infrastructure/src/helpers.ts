import { Repository } from "aws-cdk-lib/aws-ecr";
import { devConfig } from "./config/dev";
import { qaConfig } from "./config/qa";
import { prodConfig } from "./config/prod";
import { App, RemovalPolicy, Stack, StackProps } from "aws-cdk-lib";
import { IVpc, Peer, Port, SecurityGroup, Vpc, SubnetType} from "aws-cdk-lib/aws-ec2";


/**
 * Helpers
 */

const env = process.env.ENV ?? 'dev';
const config = env === 'prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;

export const getTransfloVpc = (scope: Stack) => {
    return Vpc.fromLookup(scope, "transflo-vpc", { vpcId: config.vpcId });
};

export const createSimpleEcrRepository = (scope: Stack, name: string) => {
    return new Repository(scope, name, {
        repositoryName: name,
        removalPolicy: RemovalPolicy.DESTROY,
        emptyOnDelete: true,
    })
};
