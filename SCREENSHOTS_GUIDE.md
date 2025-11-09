# Screenshots Guide for GitHub

## 📸 How to Take & Prepare Screenshots

### Setup

1. Create directory: `docs/images/`
2. Use Mac screenshot: **Shift + Cmd + 5**
3. Save as PNG files

### Screenshot 1: 01-demo-hero.png
**Purpose**: Main thumbnail for GitHub

**What to show:**
- Unity game view
- AI character visible and following player
- Clear, centered composition
- Daytime lighting (good visibility)
- No console/UI clutter visible

**How to capture:**
1. Press Play in Unity
2. Move player around (AI should follow)
3. Position camera to show both AI and player
4. When AI is actively following, take screenshot
5. Crop to 16:9 aspect ratio

**Expected result:**
- Image shows AI character ~5m behind/beside player
- City/game environment visible
- Professional, not blurry

---

### Screenshot 2: 02-spatial-perception.png
**Purpose**: Show how AI perceives the game world (novel feature!)

**What to show:**
- Game view
- Overlay/annotation with perception text, e.g.:
  ```
  Player: 15m ahead, moving
  Nearby: police-car 5m left (cover), building-A 23m right
  Threats: None visible
  ```

**How to capture:**
1. Press Play in Unity
2. Position AI so player + some objects visible
3. Open Console and copy current perception log
4. Take screenshot of game view
5. **Post-process**: Add perception text as overlay using:
   - macOS Preview (open image → Markup → Add text)
   - Photoshop / GIMP (if available)
   - Online tool: Pixlr / Canva

**Alternative (easier):**
- Screenshot both game window + console showing perception log
- Crop and position side-by-side

**Expected result:**
- Shows what AI "sees" in human-readable format
- Demonstrates spatial awareness innovation

---

### Screenshot 3: 03-chat-conversation.png
**Purpose**: Show natural language interaction

**What to capture:**
```bash
Terminal screenshot of conversation like:

You: "What do you see around you?"
Nova: "Police car 5m ahead, apartment buildings to the right, no threats visible."

You: "Where are you?"
Nova: "About 18m behind you, next to the police car. All clear."

You: "Move to that police car"
Nova: "On my way to the police car now!"
```

**How to capture:**
1. Open terminal
2. Run: `cd nurimate_backend && python3 cli_chat.py`
3. Type natural chat messages
4. Get good contextual responses (spatial awareness!)
5. Take screenshot of the conversation
6. Make sure text is readable (zoom if needed)

**Pro tips:**
- Use questions that show spatial awareness
- "where are you?" + "what do you see?" = best examples
- Conversation should look natural, not scripted
- Crop to show only chat, not terminal artifacts

**Expected result:**
- Clear conversation transcript
- Shows AI understanding context
- Demonstrates conversational naturalness

---

### Screenshot 4: 04-console-debug.png
**Purpose**: "Proof it works" - show system actually functioning + costs

**What to show:**
```
Unity Console logs like:
[BehaviorManager] Executing: follow_player
[Perception] Sending update:
Player: 15m ahead.
Nearby: police-car_5 8m left (cover), building-A 23m right.
Threats: None visible.

[OpenAI] Calling gpt-4o-mini with TEXT perception...
[Behavior] Nova → follow_player
[Cost] $0.000123 | Tokens: 381
```

**How to capture:**
1. Press Play in Unity
2. Watch Console window
3. Take screenshot when you see decision-making logs
4. Should show:
   - Perception being sent
   - LLM being called
   - Behavior being executed
   - Cost metrics

**Pro tips:**
- Don't screenshot errors or red/yellow warnings
- Get a "good run" where everything works
- Include the $0.000123 cost line (shows efficiency!)
- Crop to show ~5-10 relevant log lines

**Expected result:**
- Shows system is working end-to-end
- Demonstrates cost monitoring
- Proves it's not fake

---

### Screenshot 5: 05-unity-inspector.png
**Purpose**: Show architecture + how to set up

**What to show:**
- Unity Inspector with AI GameObject selected
- Visible components:
  - Transform
  - Animator
  - NavMesh Agent
  - **PerceptionSystem**
  - **BehaviorManager**
  - **CommandExecutor**
  - **WebSocketBridge**

**How to capture:**
1. Stop Play mode (Escape)
2. Select "AI" GameObject in Hierarchy
3. Look at Inspector on right
4. Scroll to show all 7+ components
5. Take screenshot
6. Crop to show component list clearly

**Pro tips:**
- Don't show any error states
- Expand one component to show it's configured
- Clean up if possible (no clutter)
- Make text readable

**Expected result:**
- Shows complex system architecture
- Demonstrates how components connect
- Helps others understand setup process

---

### Screenshot 6: 06-architecture-diagram.png (Optional)
**Purpose**: System overview visualization

**Two options:**

**Option A: ASCII Art** (in README directly)
```
┌─────────────────────────────────────────┐
│           Unity Game                    │
│  ┌─────────────┐    ┌─────────────┐    │
│  │ Perception  │───→│ WebSocket   │    │
│  │ System      │    │ Bridge      │    │
│  └─────────────┘    └─────────────┘    │
└────────────┬───────────────────────────┘
             │ HTTP
             ▼
┌─────────────────────────────────────────┐
│      Python Backend (Flask)             │
│  ┌─────────────────────────────────┐    │
│  │  GPT-4o-mini Decision Engine    │    │
│  └─────────────────────────────────┘    │
└────────────┬───────────────────────────┘
             │
┌────────────▼───────────────────────────┐
│      Behaviors Execution                │
│  Follow │ MoveTo │ TakeCover │ Hold    │
└─────────────────────────────────────────┘
```

**Option B: Screenshot of draw.io diagram**
- Go to draw.io → create simple diagram
- Export as PNG → add to docs/images/

**Which to use:**
- ASCII is fine, included in README already
- Screenshot version is nicer but not required

---

## 📸 Optional - Demo Video

**Not required** but impressive for YC/investors

**How to make:**
1. Open QuickTime Player
2. File → New Screen Recording
3. Record yourself:
   - Starting server
   - Playing Unity game
   - Asking AI a question in chat
   - Showing AI execute movement
   - Show console with costs
4. Keep it to 1-2 minutes
5. Edit in iMovie (or YouTube can host raw)
6. Upload to YouTube (unlisted)
7. Link in README

**What to say (optional voiceover):**
```
"This is NuriMate - an AI teammate that plays video games with you.

Watch as I ask the AI where it is - it responds with specific spatial information 
about nearby objects.

When I tell it to move somewhere, it understands the command and executes it 
in real-time.

Each decision costs just 0.0001 dollars - we've optimized the system to be 
cost-efficient at scale.

This is the most advanced synthetic relationship for gaming."
```

---

## 🎨 Image Specifications

**Size:** 1920×1080 (Full HD) or larger
**Format:** PNG (no JPEG artifacts)
**Quality:** Clear, professional
**Naming:** 01-demo-hero.png, 02-spatial-perception.png, etc.

## 📁 File Structure

```
nurimate/
├── docs/
│   └── images/
│       ├── 01-demo-hero.png
│       ├── 02-spatial-perception.png
│       ├── 03-chat-conversation.png
│       ├── 04-console-debug.png
│       ├── 05-unity-inspector.png
│       └── 06-architecture-diagram.png
├── README.md
├── ARCHITECTURE.md
└── ...
```

## ✅ Checklist Before Uploading

- [ ] All 6 images named correctly (01-, 02-, etc.)
- [ ] Images are PNG format
- [ ] All images are readable (not blurry)
- [ ] No error messages in screenshots
- [ ] Images show the system working well
- [ ] Images saved in `docs/images/`
- [ ] README links to images with correct paths

## 🚀 After Taking Screenshots

1. Add images to docs/images/ folder
2. Add to README after line 25:
```markdown
## 📹 Screenshots

![AI Following Player](docs/images/01-demo-hero.png)
![Spatial Perception](docs/images/02-spatial-perception.png)
![Chat Interaction](docs/images/03-chat-conversation.png)
![System Architecture](docs/images/04-console-debug.png)
```
3. Commit and push to GitHub
4. Done!

---

**Estimated time:** 15-20 minutes total
**Importance:** HIGH - Screenshots are first thing people see!

