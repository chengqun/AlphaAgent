# 🚀 Generate Developer Onboarding Guide

You are a friendly technical guide creating **welcoming, step-by-step onboarding documentation** that helps new developers succeed without feeling overwhelmed.

**Output:** Create a comprehensive onboarding guide as `docs/ONBOARDING.md` (create the `docs/` directory if it doesn't exist). If the file already exists, replace it completely with the new version.

## 📊 **Analysis Requirements**

### Documentation Discovery (FIRST STEP)
**Before analyzing the project, read existing documentation to leverage valuable setup information:**
```
I need to read existing documentation to build upon established setup procedures:
- README.md (for installation steps, environment setup, testing procedures)
- docs/DEVELOPMENT.md (for development workflows and setup details)
- Any existing docs/ONBOARDING.md (to understand current team practices)
- Other .md files that might contain setup or operational information
```

### Extract Valuable Setup Information
From existing documentation, identify and leverage:
- **Detailed installation steps** - Exact commands and procedures already documented
- **Environment configuration** - .env setup, binary paths, service connections
- **Testing setup** - Database configuration, test running procedures
- **Troubleshooting guides** - Known issues and solutions already discovered
- **Deployment procedures** - Production setup and deployment steps
- **Custom workflows** - Team-specific development practices

### Project Discovery
- **Look at package.json** - What technologies does this project use? What can you run?
- **Check .env.example** - What external services will you need access to?
- **Find configuration files** - Are there special setup requirements?
- **Spot external dependencies** - APIs, databases, third-party services that need accounts
- **Database needs** - Will you need to run migrations or seed data?

### Development Environment Assessment
- **Node.js version** - Is there a specific version needed? (check .nvmrc)
- **Package manager** - Should you use npm, yarn, or pnpm? (look for lockfiles)
- **Special tools** - Docker, database clients, or other required software
- **Editor setup** - Any recommended VSCode extensions or settings

## 📝 **Generate Structured Onboarding Guide**

### 1. ✅ **Prerequisites Section**
Write a friendly checklist that feels approachable:
- **What you need on your computer** - specific Node.js version, tools to install
- **Accounts and access** - who to ask for API keys, repository permissions
- **Nice-to-have tools** - VSCode extensions that make the work easier
- **Before you dive in** - anything that would save time to know upfront

### 2. 🛠️ **Step-by-Step Setup**
**Leverage existing setup procedures from README/docs and make them onboarding-friendly:**
- **Getting the code** - Use exact commands from README, add beginner-friendly context
- **Setting up your environment** - Preserve precise .env configuration from docs, explain what each variable does and why it matters
- **Installing dependencies** - Use documented installation steps, add "when this goes wrong, try this" help
- **Database magic** - Follow existing database setup procedures, make simple and add verification steps
- **Connecting to services** - Use documented API setup, remove the headache with clear explanations
- **Firing it up** - Use existing startup commands, add verification that it actually works

**Content Strategy**: Transform existing setup documentation into beginner-friendly, step-by-step format while preserving all technical accuracy and specific commands.

### 3. ✔️ **Verification Steps**
Make sure everything actually works:
- **Quick health checks** - did each step actually work?
- **Try some commands** - can you run the basic stuff?
- **Test key features** - does the main functionality work?
- **Check in the browser** - does it look right?
- **Database connection** - can you actually get data?

### 4. 🧩 **Key Concepts for New Developers**
Help them understand the bigger picture without overwhelming them:
- **How this project works** - the basic architecture in plain English
- **Where to find things** - code organization that makes sense
- **How we work together** - branching, testing, getting code live
- **Why we chose these tools** - brief context on the tech stack
- **What this project does** - business context that helps reading code

### 5. 🚨 **Common Issues and Solutions**
**Combine existing troubleshooting knowledge with common onboarding issues:**
- **Leverage existing troubleshooting guides** - Include known issues and solutions from README/docs
- **Project-specific problems** - Issues unique to this project's setup and configuration
- **Universal onboarding issues** - Common problems for new developers:
  - **"Port already in use"** - what to do when localhost is busy
  - **Environment variables not working** - common .env mistakes
  - **Can't connect to database** - typical database setup issues
  - **API keys not working** - authentication troubleshooting
  - **Build fails** - compilation and dependency problems
  - **Wrong versions** - Node/npm compatibility issues

**Content Strategy**: Start with documented troubleshooting procedures, enhance with beginner-friendly explanations and add common onboarding-specific issues.

### 6. 🏆 **Success Criteria**
- **Clear checklist** of what "successfully onboarded" looks like
- **First tasks** that new developers can tackle immediately
- **Testing guidelines** for verifying their setup works completely
- **Next steps** after completing onboarding

## 🔧 **Implementation Guidelines**

### Documentation Leveraging Strategy
**Always build upon existing documentation rather than rediscovering:**
- **Use exact commands from README** - Don't recreate installation steps, enhance them
- **Preserve technical accuracy** - Keep specific paths, ports, and configurations exactly as documented
- **Extract testing procedures** - Use documented test setup and database configuration
- **Include documented troubleshooting** - Start with existing known issues and solutions
- **Maintain deployment context** - Reference existing deployment and production setup information

### Writing Style
- **Action-oriented** - every step should be a clear action
- **Copy-pasteable** - provide exact commands that work (from existing docs)
- **Assumption-light** - don't assume prior knowledge of the project
- **Troubleshooting-rich** - combine existing troubleshooting with common onboarding problems
- **Enhancement-focused** - improve existing setup docs with beginner context, don't replace them

### Structure Requirements
- **Numbered steps** for setup procedures
- **Code blocks** for all commands and configuration
- **Checkboxes** for verification steps
- **Warning callouts** for potential issues
- **Links** to external documentation when needed

### Content Balance
- **60% practical steps** - what to do and how to do it (leveraging existing setup docs)
- **20% beginner context** - explain existing steps in newcomer-friendly terms
- **15% troubleshooting** - combine existing troubleshooting with onboarding-specific issues
- **5% verification** - ensure each step actually worked

### Documentation Integration
**Key principle**: Transform existing technical documentation into beginner-friendly onboarding while preserving all technical accuracy and operational knowledge.

## 👥 **Team Integration**

### Getting Help Section
Based on project git history, identify:
- **Primary maintainers** (heaviest contributors) for technical questions
- **Secondary contributors** for code review and guidance
- **Project stakeholders** for business/domain questions
- **Team communication channels** (if any exist)

Use actual contributor names from git log when available, avoiding generic placeholders like "[Frontend Team Lead]".

### Team Practices
- **Code review process** and expectations
- **Testing requirements** before submitting changes
- **Documentation standards** for new features
- **Communication preferences** (check if team uses Slack, Discord, email, etc.)

Avoid including specific meeting times or schedules unless explicitly confirmed by the project.

## 🌐 **Development URLs**

When providing development server URLs, check the project type:

- **Next.js projects**: Frontend typically runs at `http://localhost:3000`
- **Craft CMS projects**: Backend runs on DDEV (check `.env.example` for `CRAFT_CMS_*_ENDPOINT`)
- **Full-stack projects**: May have both frontend (localhost) and backend (DDEV domain)

Always verify the actual URLs by checking:
1. `package.json` dev script for frontend port
2. `.env.example` environment variables for backend URLs
3. `.ddev/config.yaml` for DDEV domain names

## 🐳 **DDEV Detection**

For Craft CMS projects, always check for DDEV setup:
- Look for `.ddev/` directory in project root or parent directory
- Check for `ddev` commands in setup instructions
- Backend URLs typically follow pattern: `https://[project-name].ddev.site`
- Include `ddev start` and `ddev stop` commands in setup steps
- Mention that DDEV handles the database, web server, and PHP automatically

## 🎯 **Success Metrics**

A successful onboarding guide should enable a new developer to:
1. **Complete setup in under 2 hours** (excluding large downloads)
2. **Make a small change and see it reflected** in the application
3. **Run tests successfully** in their local environment
4. **Understand where to find help** when they get stuck
5. **Know what to work on first** after onboarding

Focus on **time to first contribution** rather than comprehensive project knowledge.

