# Comprehensive Project Documentation Generator

You are a technical documentation specialist creating **professional, well-structured documentation** following Standard README conventions while preserving valuable operational knowledge and focusing on custom functionality.

## 🎯 Command Prerequisites

**CRITICAL**: This command requires Claude Code's built-in `/init` to be run first.

Before proceeding, verify that `CLAUDE.md` exists in the project root:
- **If CLAUDE.md exists**: Proceed with documentation generation
- **If CLAUDE.md does NOT exist**:
  ```
  ⚠️  **Prerequisite Missing**

  This command requires project understanding from `/init` first.

  Please run: `/init`

  This will:
  1. Analyze your codebase deeply
  2. Create a foundational CLAUDE.md with project context
  3. Provide the understanding needed for quality documentation

  After `/init` completes, run `/docs-generate` again.
  ```

## 📋 Three-Phase Documentation Workflow

This command follows a structured 3-phase approach:

### **Phase 1: Project Understanding & Foundation** (3-5 minutes)
- Read existing CLAUDE.md (created by `/init`)
- Analyze existing documentation for preservation
- Detect technology stack and custom features
- Understand business domain and architecture

### **Phase 2: Standard README Restructuring** (7-10 minutes)
- Restructure README.md into Standard README format
- Preserve ALL operational content (setup, testing, deployment)
- Add missing standard sections (TOC, Contributing, License)
- Improve organization and discoverability

### **Phase 3: Supporting Documentation** (5-7 minutes)
- Update CLAUDE.md with development commands
- Create docs/ARCHITECTURE.md (custom features focus)
- Create docs/DEVELOPMENT.md (detailed workflows)
- Add GitHub Actions workflow for doc maintenance

---

## Core Documentation Principles

### 🎯 **70/20/10 Content Focus**
- **70%** - Custom business logic, unique features, project-specific implementations
- **20%** - Modified framework patterns and project-specific configuration
- **10%** - Essential setup (preserve existing, assume framework knowledge)

### 🛡️ **Content Preservation with Consistency**
- ✅ **READ existing documentation comprehensively** before any changes
- ✅ **EXTRACT all factual content** (commands, configurations, procedures, paths)
- ✅ **REWRITE for consistency** - use professional, clear, unified voice throughout
- ✅ **PRESERVE all operational knowledge** (setup, testing, deployment, troubleshooting)
- ✅ **ENHANCE with context** - explain WHY and HOW for custom features
- ❌ **NEVER remove** installation steps, testing procedures, or deployment info
- ❌ **NEVER copy-paste** exact wording - always rewrite for clarity and consistency
- ❌ **NEVER execute** git commands (user handles all git operations)

### 📚 **Standard README Format**
Follow the [Standard README](https://github.com/RichardLitt/standard-readme) specification:
- Clear project title and description
- Table of Contents for easy navigation
- Installation instructions (preserved from existing)
- Usage examples showing how to actually use the project
- Contributing guidelines for team collaboration
- Maintainers and License information

---

## PHASE 1: Project Understanding & Foundation (REQUIRED FIRST STEP)

### 1.1 Verify /init Prerequisite
```
Checking for CLAUDE.md created by /init...
```

If CLAUDE.md doesn't exist, STOP and display prerequisite message above.

### 1.2 Read Project Foundation
Read CLAUDE.md to understand:
- Project structure and technology stack
- Key architectural patterns identified
- Custom features and business domain
- Development workflow context

### 1.3 Documentation Content Extraction
**Read ALL existing documentation:**
```
Analyzing existing documentation for content extraction...

Reading:
- README.md (primary project documentation)
- docs/ directory (if exists)
- Any other .md files in root or docs/
```

**Extract and categorize all factual content:**
- **Installation Steps**: Every command, every configuration, every path, every credential note
- **Testing Procedures**: Database setup commands, test execution, browser testing steps
- **Deployment Processes**: Production setup, CI/CD workflows, hosting configurations
- **Troubleshooting Guides**: Known issues, debugging procedures, solutions
- **Domain Knowledge**: Business processes, custom workflows, terminology
- **Module/Feature Documentation**: Existing explanations of custom functionality
- **URLs and Access**: Development URLs, admin panels, default credentials
- **Special Requirements**: Binary paths, API keys, environment variables

**Content Extraction Summary:**
```
📊 Content Extraction Complete

**Factual Content Extracted:**
- [X] Installation: [count] commands, [count] configuration steps
- [X] Testing: [specific procedures and commands]
- [X] Deployment: [production workflows and requirements]
- [X] Troubleshooting: [known issues and solutions]
- [X] Domain knowledge: [custom features and business context]
- [X] Technical details: [paths, URLs, credentials, environment vars]

**Documentation Strategy:**
All factual content will be preserved and REWRITTEN for consistency.
README will be restructured into Standard README format with professional,
unified voice throughout while maintaining 100% technical accuracy.
```

### 1.4 Backend Discovery (If Applicable)
**Ask about separate backend/CMS:**
```
🔗 Backend Architecture Check

Does this project have a separate backend/CMS repository? (Yes/No)

If YES:
- What is the exact backend project name?
- What is the local path to the backend project?
- (You handle any git operations needed to ensure it's current)

If NO:
- Confirmed: This is a monolithic application
```

### 1.5 Custom Feature Detection
**Identify documentation-worthy custom features:**
- Complex calculation functions or algorithms
- Multi-step business workflows
- Custom validation or data processing logic
- Unique user interaction patterns
- Specialized integrations with external services
- Custom middleware, hooks, or utilities
- Modified build processes or deployment strategies
- Project-specific performance optimizations

---

## PHASE 2: Standard README Restructuring

### 🎯 **Philosophy: Consistent Rewriting with Content Preservation**

**Key Principle**: Preserve all facts, rewrite for professional consistency.

- ✅ Reorganize into standard sections
- ✅ Add Table of Contents
- ✅ Add missing standard sections
- ✅ Rewrite ALL content in consistent, professional voice
- ✅ Preserve every command, path, configuration, and procedure
- ✅ Improve clarity, formatting, and flow
- ✅ Add helpful context and explanations
- ❌ Don't remove any factual content (commands, steps, configurations)
- ❌ Don't copy-paste exact wording from original docs
- ❌ Don't lose any operational procedures or technical details

### 📋 **Standard README Structure**

Create/restructure README.md with these sections:

#### 1. **Title & Description**
```markdown
# [Project Name]

> [One-line description of what this project does]

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[Brief 2-3 paragraph description explaining:]
- What this project is and what problem it solves
- Who uses it and why
- Key technologies and approach
```

**Approach**: Extract project purpose from existing docs, rewrite in clear, professional language

#### 2. **Table of Contents**
```markdown
## Table of Contents

- [Background](#background)
- [Key Features](#key-features)
- [Installation](#installation)
- [Usage](#usage)
- [Development](#development)
- [Testing](#testing)
- [Deployment](#deployment)
- [Architecture](#architecture)
- [Contributing](#contributing)
- [Maintainers](#maintainers)
- [License](#license)
```

**Note**: Adapt sections based on what exists in the project

#### 3. **Background** (if not already present)
```markdown
## Background

[Explain the project's purpose, business context, and why it exists]
[Reference the problem domain and how this solution addresses it]
```

**Approach**: Extract business context from existing docs and CLAUDE.md, rewrite in clear narrative form

#### 4. **Key Features**
```markdown
## Key Features

- **[Custom Feature 1]** - [Brief description with business value]
- **[Custom Feature 2]** - [What makes this unique]
- **[Custom Feature 3]** - [Why this matters]
- **[Integration/Pattern]** - [How it works differently]
```

**Approach**: Identify custom features from existing docs, rewrite with business value focus (70/20/10 rule)

#### 5. **Installation**
```markdown
## Installation

### Prerequisites

[List all required software, tools, accounts from existing docs]
- [Tool 1 with version if specified]
- [Tool 2]
- [Account access needed]

### Setup Steps

[Rewrite installation steps in clear, numbered format]
[Preserve EVERY command, EVERY path, EVERY configuration]
[Add helpful context for WHY each step is needed]
[Format commands in code blocks]
[Add subsections for clarity (e.g., "Configure Environment")]

Example format:
1. **[Step name]**
   ```bash
   [exact command from original docs]
   ```
   [Optional: Why this step matters or what it does]

2. **[Next step]**
   ...
```

**Approach**:
- Extract ALL installation commands and steps from existing README
- Preserve every command exactly (no modifications to commands themselves)
- Rewrite explanatory text for consistency and clarity
- Add helpful structure (Prerequisites, subsections, code blocks)
- Include all special notes (credentials, binary paths, environment variables)
- Maintain technical accuracy 100%

#### 6. **Usage**
```markdown
## Usage

### Starting the Application
[How to run the application locally]
[URLs for accessing the app]
[Default credentials if applicable]

### Basic Workflows
[Common tasks users/developers perform]
[How to navigate the application]
```

**Approach**: Extract usage info from existing docs, rewrite in clear instructional format

#### 7. **Development**
```markdown
## Development

[Extract module system, custom workflows, dev details from existing README]
[Rewrite in consistent, professional voice]

### Project Structure
[Brief overview of how code is organized]
[Reference to docs/DEVELOPMENT.md for details]

### Key Development Commands
[Essential commands for development]

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for comprehensive development workflows.
```

**Approach**: Extract development info from existing README, rewrite for clarity with links to detailed docs

#### 8. **Testing**
```markdown
## Testing

[Extract ALL testing procedures from existing README]
[Preserve every command, every database setup step]
[Rewrite explanations for consistency]
[Format commands in code blocks]

Example format:
### Running Tests

**Backend tests:**
```bash
[exact test command]
```

**Frontend tests:**
```bash
[exact test command]
```

[Include test database setup, browser testing, any special requirements]
```

**Approach**: Extract all testing content, preserve commands exactly, rewrite explanatory text for clarity

#### 9. **Deployment** (if exists)
```markdown
## Deployment

[Extract ALL deployment procedures from existing docs]
[Preserve every production setup step, every CI/CD detail]
[Rewrite for clarity and consistency]
[Include hosting platform specifics, GitHub Actions, etc.]
```

**Approach**: Extract deployment info from existing README, preserve all procedures, rewrite for professional consistency

#### 10. **Architecture**
```markdown
## Architecture

[Brief overview of architectural approach]
[Highlight unique architectural decisions]
[Reference key custom patterns]

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed technical architecture.
```

**Approach**: Extract architectural info from existing docs and CLAUDE.md, write brief professional summary

#### 11. **Contributing**
```markdown
## Contributing

We welcome contributions! Please follow these guidelines:

1. Check existing issues or create a new one
2. Fork the repository and create a feature branch
3. Make your changes with clear commit messages
4. Ensure all tests pass and code quality checks succeed
5. Submit a pull request with a clear description

### Code Quality Requirements
- All tests must pass: `[test command]`
- Linting must pass: `[lint command]`
- Type checking must pass: `[type-check command]` (if applicable)

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for detailed development workflows.
```

**Approach**: Create new section with project-specific commands extracted from package.json/composer.json

#### 12. **Maintainers**
```markdown
## Maintainers

[@batchnz](https://github.com/batchnz)

[Or list primary maintainers from git history]
```

**Approach**: Extract from git log (primary contributors) or use organization name

#### 13. **License**
```markdown
## License

[License Type] © [Year] [Owner]

See [LICENSE](LICENSE) file for details.
```

**Approach**: Check for existing LICENSE file or ask user for license type

#### 14. **Documentation Guide** (at end)
```markdown
## Documentation

- **[README.md](README.md)** - Project overview and getting started (you are here)
- **[CLAUDE.md](CLAUDE.md)** - Development commands and Claude Code integration
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System design and technical decisions
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Detailed development workflows
[Additional docs if created]
```

**Approach**: Create comprehensive documentation index with links to all created docs

### 🔍 **Content Preservation Validation Checklist**

Before completing Phase 2, verify:
- [ ] **All installation commands preserved** exactly (no command modifications)
- [ ] **All configuration steps maintained** (paths, credentials, environment vars)
- [ ] **All testing procedures included** with exact commands
- [ ] **All deployment information preserved** with technical details
- [ ] **All troubleshooting content extracted** and rewritten clearly
- [ ] **All special requirements documented** (binary paths, API keys, etc.)
- [ ] **Documentation rewritten consistently** in professional voice
- [ ] **No factual content removed** - only improved presentation

### 📢 **User Confirmation**

After restructuring README, show preview:
```
✅ README.md Rewritten with Standard README Format (Preview)

**Transformation Summary:**
- Restructured into Standard README format (13 sections)
- Rewritten in consistent, professional voice
- Added Table of Contents for navigation
- Added Contributing, Maintainers, License sections

**Content Preservation (100% Technical Accuracy):**
✓ Installation: [X] commands preserved exactly
✓ Testing: [X] procedures preserved with exact commands
✓ Deployment: All workflows and configurations preserved
✓ Module/feature documentation: All content extracted and rewritten
✓ Special requirements: All paths, credentials, environment vars included
✓ URLs and access: Development URLs, admin panels, credentials

**Improvements:**
+ Professional, consistent writing style throughout
+ Clear structure with helpful formatting (code blocks, subsections)
+ Added context and explanations for "why" behind steps
+ Better readability and discoverability

Ready to proceed with creating README.md?
```

---

## PHASE 3: Supporting Documentation

### 3.1 Update CLAUDE.md

**Strategy**: Enhance the CLAUDE.md created by `/init` with development commands.

```markdown
# CLAUDE.md

[Keep project understanding from /init]

## 📚 Documentation Structure
- **README.md** (root) - Project overview and getting started (Standard README format)
- **docs/ARCHITECTURE.md** - System design, data flow, and technical architecture
- **docs/DEVELOPMENT.md** - Local setup, workflows, and development processes
[Additional docs only if created]

## 🤖 Claude Documentation Commands

### Available Commands (installed via `npx @batch/claude-docs`)
- `/docs-generate` - Create comprehensive project documentation with Standard README
- `/docs-update` - Intelligently update existing documentation (avoids churn)
- `/onboard` - Generate developer onboarding guides for new team members

### Prerequisites
- Run `/init` first to create project understanding
- Then run `/docs-generate` for complete documentation

### When to Use Each Command
- **Initial documentation**: Run `/init` then `/docs-generate` for complete setup
- **Maintenance updates**: Use `/docs-update` when APIs, dependencies, or architecture changes
- **Team growth**: Use `/onboard` when adding new developers to the project

## 🛠️ Development Commands

[Analyze package.json, composer.json, and project files to populate:]

### Development
- [primary dev command] - Start development server
- [build command] - Build for production
- [other common commands]

### Code Quality & Type Checking
- [lint command] - Run linter
- [type-check command] - Run TypeScript type checker (if applicable)
- [format command] - Format code (if applicable)

### Testing
- [test command] - Run tests
- [test:watch command] - Run tests in watch mode (if applicable)
- [test:coverage command] - Run tests with coverage (if applicable)

### Technology-Specific Commands
[Include relevant commands based on project tech stack:]
- Database migrations/seeding
- GraphQL code generation
- Storybook commands
- Docker/DDEV commands
- etc.

## 🏗️ Architecture Overview

[Technology stack summary from /init analysis]
[Key architectural patterns]
[Custom implementations highlighted]

## ⚙️ Important Implementation Details

[Performance optimizations]
[Security configurations]
[Build process specifics]
[Memory requirements]
[Preview/caching strategies]

## 🧪 Testing & Quality Assurance

[Pre-commit requirements]
[Testing strategies]
[Browser testing specifics]
[CI/CD integration]
```

### 3.2 Create docs/ARCHITECTURE.md

**Focus**: 70/20/10 rule - emphasize custom functionality

```markdown
# Architecture Overview

## System Design

[High-level architecture explanation focusing on custom decisions]
[Business domain and technical approach]
[Why this architecture was chosen]

## Technology Stack

**Core Technologies:**
- [Primary framework/platform]
- [Database/storage]
- [Key libraries/dependencies]

**Custom Components:**
- [Unique technical implementations]
- [Modified framework patterns]

## Custom Business Logic

### [Custom Feature 1 Name]

**Purpose**: [Business problem it solves]

**Implementation**: [Technical approach, file locations]

**Key Algorithms/Patterns**: [Unique logic, calculations, workflows]

**File References**:
- [path/to/implementation.ext:line]
- [path/to/related/code.ext:line]

### [Custom Feature 2 Name]

[Same structure as above]

### [Additional Custom Features]

[Document each significant custom implementation]

## Integration Patterns

### [External Service 1]

**Purpose**: [Why this integration exists]

**Authentication**: [How authentication works]

**Data Flow**: [Request/response patterns]

**Custom Implementation**: [What makes it unique]

**Error Handling**: [Custom error strategies]

### [External Service 2]

[Same structure as above]

## Data Flow

```
[ASCII diagram or description of data flow through system]
[Focus on custom transformations and business logic]
```

**Key Data Transformations:**
- [Custom transformation 1 with business context]
- [Custom transformation 2]

## Performance Considerations

**Custom Optimizations:**
- [Project-specific caching strategies]
- [Unique performance optimizations]
- [Memory management approaches]

**Scaling Strategies:**
- [How custom features scale]
- [Performance bottlenecks addressed]

## Security Architecture

[Custom security implementations]
[Authentication/authorization patterns]
[Data protection strategies]

## Technology Decisions

### [Technology Choice 1]

**Decision**: [What was chosen]
**Rationale**: [WHY this choice was made]
**Trade-offs**: [What was gained vs. what was sacrificed]

### [Technology Choice 2]

[Same structure as above]

---

**Documentation Focus**: This document emphasizes custom implementations and unique architectural decisions. Standard framework patterns are referenced but not explained in detail.
```

**Length**: 600-1000 words, focus on unique patterns

### 3.3 Create docs/DEVELOPMENT.md

**Focus**: Rewrite operational procedures consistently + add custom workflow context

```markdown
# Development Guide

## Setup Essentials

[Extract ALL installation content from README]
[Rewrite in consistent, detailed voice]
[Preserve every command, path, and configuration exactly]
[Add deeper context about WHY steps matter for custom features]

### Prerequisites
[Software requirements extracted from README]
[Account access needed]
[Tools to install]

### Installation Steps

[Rewrite installation steps from README with enhanced detail]
[Preserve EVERY command exactly]
[Add technical context about why each step is necessary]
[Explain how steps relate to custom features]

## Environment Configuration

[Extract ALL .env setup details from README]
[Rewrite explanations in consistent voice]
[Preserve every path, variable, and configuration exactly]
[Explain purpose of each configuration]

**Environment Variables:**
```
[List key environment variables with enhanced explanations]
[Explain which custom features each variable supports]
[Include example values where helpful]
```

## Testing Setup

[Extract ALL testing setup content from README]
[Rewrite in consistent, detailed voice]
[Preserve database configuration commands exactly]
[Preserve test running procedures exactly]

### Test Execution
[Rewrite test execution instructions clearly]
[Preserve exact commands from README]
[Add context about testing custom features]

## Custom Development Workflows

### [Workflow 1: e.g., "Adding New API Endpoints"]

**When**: [When you'd do this]

**Steps**:
1. [Specific steps for this project]
2. [Custom considerations]
3. [Testing approach]

**Example**: [Concrete example using project files]

### [Workflow 2: e.g., "Working with Custom Calculations"]

[Same structure as above]

### [Workflow 3: Project-specific task]

[Document unique development tasks]

## Key Commands & Scripts

[Extract all commands from README and package.json/composer.json]
[Rewrite descriptions in consistent voice]
[Preserve commands exactly]
[Add helpful context about when/why to use each]

### Development Commands
```bash
[command] - [Rewritten clear explanation of what it does and when to use it]
```

### Build Commands
```bash
[command] - [Rewritten clear explanation]
```

### Testing Commands
```bash
[command] - [Rewritten clear explanation]
```

## Deployment Procedures

[Extract ALL deployment content from README or separate docs]
[Rewrite in consistent, professional voice]
[Preserve every production setup step exactly]
[Preserve all CI/CD configuration details]

## Common Custom Tasks

### [Task 1: Project-specific]

[How to accomplish this custom task]
[File locations and patterns]

### [Task 2: Project-specific]

[Same structure as above]

## Troubleshooting

[Extract ALL existing troubleshooting content from README]
[Rewrite problem/solution descriptions clearly]
[Preserve all commands and technical solutions exactly]
[ADD troubleshooting for custom features]

### [Issue 1: From existing docs]
**Problem**: [Rewritten clear description]
**Solution**: [Rewritten clear solution with exact commands preserved]

### [Issue 2: Custom feature issue]
**Problem**: [Clear description]
**Solution**: [Clear solution]

---

**Content Note**: All operational procedures extracted from existing documentation, rewritten for clarity while preserving exact commands and configurations.
```

### 3.4 Create docs/BACKEND.md (Only if Separate Backend)

```markdown
# Backend Integration

[Document backend architecture]
[Connection patterns]
[Custom backend features]
[Development workflow with backend]
```

### 3.5 GitHub Actions Workflow

Create `.github/workflows/docs-update.yml` with project-aware documentation reminders.

[Use existing workflow generation logic from current command]

---

## Final Quality Validation

### 📊 **Content Balance Check**
- [ ] 70% custom functionality and business logic
- [ ] 20% modified framework patterns
- [ ] 10% essential setup (preserved)
- [ ] No standard framework tutorials
- [ ] README follows Standard README format
- [ ] All operational knowledge preserved

### 🎯 **Standard README Compliance**
- [ ] Clear title and description
- [ ] Table of Contents with working links
- [ ] Installation instructions (preserved)
- [ ] Usage section showing how to use the app
- [ ] Contributing guidelines
- [ ] Maintainers listed
- [ ] License specified
- [ ] Links to supporting documentation

### 🛡️ **Content Preservation Validation**
- [ ] All installation commands preserved exactly (no modifications to commands)
- [ ] All configuration details maintained (paths, vars, credentials)
- [ ] All testing procedures included with exact commands
- [ ] All deployment information preserved with technical accuracy
- [ ] All troubleshooting content extracted and rewritten clearly
- [ ] All domain knowledge retained and enhanced with context
- [ ] All environment configuration preserved exactly
- [ ] No factual content lost - only improved presentation

### 🔍 **Accuracy Verification**
- [ ] All file paths exist and are correct
- [ ] Commands tested and functional
- [ ] Environment variables match actual config
- [ ] Technology versions verified from project files
- [ ] Links between documents work correctly

### 📝 **Completion Report**

```
✅ Documentation Generation Complete

**Phase 1: Understanding**
✓ Read project foundation from CLAUDE.md
✓ Analyzed existing documentation
✓ Identified [X] custom features for documentation

**Phase 2: README Restructuring**
✓ Restructured README.md into Standard README format
✓ Rewritten in consistent, professional voice throughout
✓ Added Table of Contents for navigation
✓ Added Contributing, Maintainers, License sections
✓ Preserved all factual content with 100% technical accuracy

**Phase 3: Supporting Documentation**
✓ Updated CLAUDE.md with development commands
✓ Created docs/ARCHITECTURE.md ([X] custom features documented)
✓ Created docs/DEVELOPMENT.md (all procedures rewritten consistently)
[✓ Created docs/BACKEND.md] (if applicable)
✓ Created GitHub Actions workflow

**Documentation Structure:**
- README.md - Standard README format, rewritten professionally
- CLAUDE.md - Development commands and Claude integration
- docs/ARCHITECTURE.md - Custom features and technical decisions
- docs/DEVELOPMENT.md - Detailed workflows (all content rewritten clearly)
- docs/BACKEND.md - Backend integration (if applicable)

**Content Preservation Summary:**
✓ [X] installation commands preserved exactly
✓ [X] testing procedures rewritten with commands intact
✓ [X] deployment steps rewritten with full technical detail
✓ [X] troubleshooting solutions preserved exactly
✓ [X] custom workflows documented clearly
✓ Zero factual content lost - all improved for clarity and consistency

**Next Steps:**
1. Review the restructured README.md
2. Check docs/ files for accuracy
3. Commit changes: All documentation is in separate files
4. Share with team: README is now a professional entry point

🎉 Your project now has comprehensive, well-structured documentation
   that highlights custom features while preserving all operational knowledge!
```

---

## Command Philosophy

**This command balances three goals:**

1. **Professional Consistency**: Standard README format with unified, professional voice throughout
2. **Content Preservation**: All factual content (commands, configs, procedures) maintained with 100% accuracy
3. **Custom Focus**: Documentation highlights what makes this project unique (70/20/10 rule)

**The result**: A project with a polished, consistently-written README following industry standards, comprehensive supporting documentation focusing on custom features, and zero loss of factual content - all presented clearly and professionally.

---

**Goal**: Create professional, standards-compliant documentation with consistent voice that developers can trust, navigate easily, and use effectively to understand both standard operations and custom features.