import { devConfig } from "../../config/dev";
import { qaConfig } from "../../config/qa";
import { prodConfig } from "../../config/prod";
import * as sharedConfig from '../../config/shared';
import { Stack, StackProps } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { getTransfloVpc } from '../../helpers';
import { Cluster } from "aws-cdk-lib/aws-ecs";
import * as ecs from 'aws-cdk-lib/aws-ecs';
import { EcsServiceConstruct } from "infrastructure-templates";
import { PlatformEcrStack } from '../ecr-stack';
import { PlatformSecretsStack } from '../secrets-stack';
import { PolicyStatement } from "aws-cdk-lib/aws-iam";
import { SubnetType } from "aws-cdk-lib/aws-ec2";

const env = process.env.ENV ?? 'dev';
const config = env === 'prod' ? prodConfig : env === 'qa' ? qaConfig : devConfig;

interface PlatformEcsStackProps extends StackProps {
    ecrStack: PlatformEcrStack;
    secretsStack: PlatformSecretsStack;

}

export class transformerapiStack extends Stack {
    public usertransformerApiService: EcsServiceConstruct;

    constructor(scope: Construct, id: string, props: PlatformEcsStackProps) {
        super(scope, id, props);

        const { ecrStack, secretsStack } = props;

        // Import VPC
        const vpc = getTransfloVpc(this);
        const availableAlbSubnets = vpc.selectSubnets({
            subnetType: SubnetType.PRIVATE_WITH_EGRESS,
        }).subnets;
        const devAlbSubnetIds = env === 'dev' ? new Set(config.albSubnetIds ?? []) : undefined;
        const albSubnets = devAlbSubnetIds && devAlbSubnetIds.size > 0
            ? availableAlbSubnets.filter((subnet) => devAlbSubnetIds.has(subnet.subnetId))
            : availableAlbSubnets;

        if (env === 'dev' && albSubnets.length < 2) {
            throw new Error('ALB requires at least two dev subnets in different Availability Zones. Check albSubnetIds in config.');
        }

        // Import the Dev ECS cluster
        const devCluster = Cluster.fromClusterAttributes(this, 'ImportedCluster', {
            clusterName: `${config.ecsClusterName}`,
            clusterArn: `${config.ecsClusterArn}`,
            vpc: vpc,
        });

        // Define the Transformer API ECR repository
        const transformerApiEcrRepository = ecrStack.transformerapiEcrRepository;

        // Use secrets from SecretsStack
        const transformerSecret = secretsStack.transformerSecret;

        // Define the ECS Service using EcsServiceConstruct
        this.usertransformerApiService = new EcsServiceConstruct(this, {
            name: `platform-${config.transformerapiStackName}-ecs`,
            description: 'transformer API Service',
            vpc: vpc,
            existingCluster: devCluster,
            ecrRepository: transformerApiEcrRepository,
            imageTag: 'latest',
            memorySizeMB: 2048,
            cpuUnits: 1024,
            containerPort: 8080,
            isPublic: false,
            environmentVariables: {
                'ENVIRONMENT': `${config.env}`,
                'ASPNETCORE_ENVIRONMENT': `${config.aspnetcoreEnv}`,
            },
            loadBalancerConfig: {
                healthCheckPath: '/health',
                allowedCidrs: [sharedConfig.vpnCidr, vpc.vpcCidrBlock],
            },
            albSubnetOverride: env === 'dev'
                ? {
                    subnets: albSubnets,
                }
                : undefined,
            dnsConfig: {
                subDomain: `${config.transformerapiSubDomain}`,
                domainName: `${config.rootDomain}`,
                hostedZoneId: config.hostedZoneId,
                hostedZoneName: config.rootDomain,
            },
            secrets: {
                SHARED_SECRET: ecs.Secret.fromSecretsManager(transformerSecret),
            },
        });

        const taskDefinition = this.usertransformerApiService.taskDefinition;
        if (!taskDefinition) {
            throw new Error("Task Definition not found in ECS Service Construct!");
        }
    }
}
