import json
import os
import sys
import time
from typing import Optional

import requests


def main() -> None:
    server_url = os.getenv("NURIMATE_SERVER_URL", "http://localhost:8080")
    player_id = os.getenv("PLAYER_ID", "demo_player")
    chat_url = f"{server_url}/chat"

    print("\n" + "="*60)
    print("  NuriMate Chat (Terminal)")
    print("="*60)
    print("Type your message and press Enter. Ctrl+C to exit.\n")
    print(f"Server: {chat_url}")
    print(f"Player: {player_id}\n")

    while True:
        try:
            user_text = input("you > ").strip()
            if not user_text:
                continue

            payload = {"player_id": player_id, "text": user_text}
            
            try:
                resp = requests.post(chat_url, json=payload, timeout=15)
                
                if resp.status_code == 200:
                    data = resp.json()
                    reply = data.get("reply", "")
                    
                    # Check if there's an error field (from error handling)
                    if "error" in data:
                        print(f"\n[ERROR] {data['error']}")
                        print(f"nova > {reply}\n")
                    else:
                        print(f"nova > {reply}\n")
                        
                elif resp.status_code == 500:
                    # Server error - show details
                    try:
                        data = resp.json()
                        error_msg = data.get("error", "Unknown server error")
                        print(f"\n[SERVER ERROR] {error_msg}")
                        print("Check the backend terminal for full traceback.\n")
                    except:
                        print(f"\n[SERVER ERROR] {resp.text[:300]}\n")
                        
                else:
                    print(f"\n[HTTP ERROR {resp.status_code}] {resp.text[:200]}\n")
                    
            except requests.exceptions.Timeout:
                print("\n[ERROR] Request timed out. Backend may be slow or unresponsive.\n")
                
            except requests.exceptions.ConnectionError:
                print("\n[ERROR] Could not connect to backend server.")
                print(f"Is the server running at {server_url}?\n")
                
            except Exception as e:
                print(f"\n[ERROR] Unexpected error: {e}\n")

        except KeyboardInterrupt:
            print("\n\nExiting chat. Bye!\n")
            break
        except EOFError:
            print("\n\nExiting chat. Bye!\n")
            break


if __name__ == "__main__":
    main()


