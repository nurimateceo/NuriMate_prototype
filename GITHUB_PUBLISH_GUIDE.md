# GitHub Publishing - Complete Step-by-Step Guide

## ✅ Pre-Launch Checklist

### Security Check (5 min)
- [ ] `.env` file NOT in repository (should be in `.gitignore`)
- [ ] No API keys in code files
- [ ] No hardcoded passwords or secrets
- [ ] `.gitignore` has `nurimate_backend/.env`
- [ ] `Library/`, `Logs/`, `venv/` are ignored

**Verification:**
```bash
cd "/Users/ondrejstanecka/Nurimate prototype"
# Check if .env exists locally (it should)
ls -la nurimate_backend/.env
# Should output: nurimate_backend/.env (exists)
```

### Code Quality Check (3 min)
- [ ] No compiler errors in C# (check Unity Console)
- [ ] No Python syntax errors: `python3 -m py_compile nurimate_backend/*.py`
- [ ] README is complete and accurate
- [ ] ARCHITECTURE.md is comprehensive
- [ ] All soubory are in place

### Final Content Check (2 min)
- [ ] `README.md` - Setup instructions clear ✅
- [ ] `ARCHITECTURE.md` - Technical overview complete ✅
- [ ] `LICENSE` - MIT License present ✅
- [ ] `.gitignore` - Comprehensive ✅
- [ ] `requirements.txt` - Dependencies listed ✅
- [ ] Screenshots in `/docs/images/` (optional but recommended)

**Total Pre-Launch Time: ~10 minutes**

---

## 🚀 Step-by-Step GitHub Publication

### Step 1: Create GitHub Repository (2 min)

1. Go to [github.com/new](https://github.com/new)
2. Fill in:
   - **Repository name:** `nurimate`
   - **Description:** `"The most advanced synthetic relationship - an AI friend that plays video games with you"`
   - **Public** ← SELECT THIS
   - Skip "Add a README" (you have one)
   - Skip "Add .gitignore" (you have one)
   - **License:** MIT License ← SELECT THIS
3. Click **"Create repository"**

You'll see:
```
Quick setup — if you've done this kind of thing before
```

**Copy the commands shown** (they'll be unique to your repo)

### Step 2: Initialize Git Locally (3 min)

Open Terminal in the project:

```bash
cd "/Users/ondrejstanecka/Nurimate prototype"

# Initialize git
git init

# Add all files (respects .gitignore)
git add .

# Verify what will be uploaded (DON'T include .env!)
git status
# Should show all files EXCEPT:
# - nurimate_backend/.env ✅
# - Library/ ✅
# - Logs/ ✅
# - __pycache__/ ✅

# Create initial commit
git commit -m "Initial commit: NuriMate v0.1 - AI teammate with spatial awareness"

# Rename branch (GitHub uses 'main' not 'master')
git branch -M main
```

### Step 3: Connect to GitHub (2 min)

```bash
# Use the commands GitHub gave you (substitute YOUR_USERNAME)
git remote add origin https://github.com/YOUR_USERNAME/nurimate.git
git push -u origin main
```

**Example (yours will be different):**
```bash
git remote add origin https://github.com/ondrejstanecka/nurimate.git
git push -u origin main
```

**If prompted for password:**
- Use GitHub Personal Access Token (not your password)
- Or configure SSH keys

**Expected output:**
```
Counting objects: 847, done.
Delta compression using up to 8 threads.
Compressing objects: 100% (320/330), done.
Writing objects: 100% (847/847), 45.32 MiB | 5.23 MiB/s, done.
...
To https://github.com/ondrejstanecka/nurimate.git
 * [new branch]      main -> main
Branch 'main' set to track remote branch 'main' from 'origin'.
```

### Step 4: Verify on GitHub (1 min)

1. Go to `https://github.com/YOUR_USERNAME/nurimate`
2. Refresh if needed
3. Verify:
   - ✅ All files visible
   - ✅ README shows up automatically
   - ✅ No `.env` file visible
   - ✅ No `Library/` folder visible

### Step 5: GitHub Settings (2 min)

1. Go to repo Settings (top right)
2. **Topics** → Add:
   - `ai`
   - `gamedev`
   - `unity3d`
   - `gpt-4`
   - `synthetic-relationships`
   - `ai-agents`
3. **Description** → Add (same as above)
4. Save

### Step 6: Post-Launch Marketing (5 min)

**Option 1: YC Co-founder Matching**
```
Title: Building the most advanced synthetic relationships for gaming

Description:
NuriMate - an AI teammate that understands games like a human.
- 87% cheaper than alternatives ($0.09/hour)
- Natural language perception
- Works with any game engine
- Looking for technical co-founder

GitHub: https://github.com/YOUR_USERNAME/nurimate
```

**Option 2: Social Media (Twitter/LinkedIn)**
```
🚀 Building NuriMate - The most advanced AI teammate for games

It understands spatial relationships, plays alongside you, 
and costs 87% less than alternatives.

v0.1 is live on GitHub. Looking for co-founder to help scale.

GitHub: [link]
#gamedev #ai #startup
```

**Option 3: Communities**
- Hacker News: Show HN section
- Reddit: r/gamedev, r/MachineLearning
- Discord servers focused on game dev/AI

---

## 🔒 Security Verification Checklist

Run these commands to verify security:

```bash
# Check 1: No .env in git history
git ls-files | grep -i ".env"
# Should output NOTHING ✅

# Check 2: Verify .gitignore works
cat .gitignore | grep ".env"
# Should show: nurimate_backend/.env ✅

# Check 3: Check for accidental API keys in code
grep -r "sk-" . --include="*.py" --include="*.cs"
# Should return NOTHING ✅

# Check 4: Verify large files aren't included
git ls-files --stage | awk '{print $4}' | sort -u | wc -l
# This counts tracked files (should be <1000)
```

---

## 📊 Expected Workflow

```
Pre-Launch      ← You are here
    ↓
Create GitHub Repo
    ↓
Git init + commit locally
    ↓
Push to GitHub
    ↓
Verify on GitHub
    ↓
Add topics + description
    ↓
Post to YC / Social
    ↓
Monitor stars/issues
    ↓
Respond to co-founder interest
```

---

## ⏱️ Total Time Estimate

| Step | Time |
|------|------|
| Pre-launch checks | 10 min |
| Create GitHub repo | 2 min |
| Git init + commit | 3 min |
| Push to GitHub | 2 min |
| Verify + settings | 3 min |
| Marketing post | 5 min |
| **TOTAL** | **~25 minutes** |

---

## 🎯 Success Indicators

After publishing, you should see:

**In first 24 hours:**
- ✅ GitHub repo is public and accessible
- ✅ README renders correctly
- ✅ No `.env` file visible
- ✅ File count looks reasonable (~800-1000 files)

**In first week:**
- ✅ First GitHub stars (even just 1-2)
- ✅ Maybe a GitHub issue or discussion
- ✅ Potential co-founder inquiries

**Long-term goals:**
- 50+ stars in first month
- 3+ forks
- Active discussions
- Co-founder offers

---

## 🆘 Troubleshooting

### Problem: `.env` shows in git

**Solution:** Already committed? Remove it:
```bash
git rm --cached nurimate_backend/.env
git commit -m "Remove .env from tracking"
git push
```

### Problem: Large files (Library/) are in git

**Solution:** Check .gitignore and re-track:
```bash
# Check what's there
git ls-files | head -20

# If large files present, you need to use git-lfs
# (Beyond scope - ask for help)
```

### Problem: Can't push to GitHub

**Solution:** Verify remote:
```bash
git remote -v
# Should show:
# origin  https://github.com/YOUR_USERNAME/nurimate.git (fetch)
# origin  https://github.com/YOUR_USERNAME/nurimate.git (push)
```

---

## ✅ You're Ready!

Everything checked? Time to launch! 🚀

Run the Step-by-Step Guide above and you'll be live on GitHub in ~25 minutes.

**Questions?** Check GitHub's official guide: [GitHub - Create a Repository](https://docs.github.com/en/get-started/quickstart/create-a-repo)

Good luck! 🎉

