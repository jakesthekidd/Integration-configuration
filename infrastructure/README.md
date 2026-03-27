# Platform Transformer Infrastructure CDK Project

## **Overview**
This repository contains the CDK-based infrastructure setup for the Platform Transformer project. It is organized to manage AWS resources in a modular and environment-specific manner for scalability and maintainability.

---

## **Folder Structure**

```
/infrastructure/src/
├── config                          # Environment-specific configurations
│   ├── dev.ts                     # Development environment
│   ├── qa.ts                      # QA environment
│   ├── prod.ts                    # Production environment
│   └── shared.ts                  # Shared constants/utilities
├── stacks/ECS                     # ECS stacks for Platform Transformer
├── stacks                         # Individual CDK stacks
│   ├── ecr-stack.ts               # Manages ECR repositories
│   ├── rds-stack.ts               # Provisions RDS instances
│   ├── s3-stack.ts                # Configures S3 buckets
│   └── platform-transformer-stack.ts     # Creates Platform Transformer stack
├── helper.ts                      # Shared helper functions
├── index.ts                       # Entry point for specific stack deployments

/infrastructure/
├── cdk.json                       # CDK app configuration
├── package.json                   # Node.js dependencies and scripts
├── tsconfig.json                  # TypeScript configuration
└── README.md                      # Project documentation (this file)
```

---

## **Deployment Commands**
### **Environment Variables**
Set `ENV` to the target environment (`dev`, `qa`, or `prod`).

### **Run Specific Stacks**
To deploy or test specific stacks, set the `STACK` variable to the desired stack name.

#### Example Commands:
- **Deploy ECR Stack (Development):**    --- please check package.json for the commands..

>>>> using powershell :-

$env:ENV = "dev"
$env:STACK = "LambdaStack"
npm run synth-dev <Stack_name>

>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
- **Preview Changes for Lambda Stack (QA):**
  npm run synth-qa <Stack_name>

- **Synthesize RDS Stack (Production):**
  
  npm run synth-aiprod <Stack_name>
  

### **Deploy All Stacks**
For full environment deployments, omit the `STACK` variable:
- **Deploy All Stacks (QA):**

  npm run synth-qa 
  

---

## **Current Stacks**
- **ECR Stack**: Manages Docker repositories.
- **RDS Stack**: Provisions relational databases.
- **S3 Stack**: Configures object storage.
- **ECS Stack**: Creates ECS WFAI APIs in the existed cluster.