#!/usr/bin/env bash
# Enchaîne les commandes du kit code-business-reverse en sessions Claude Code séparées (headless).
set -u
KIT="$1"; APP="$2"; LOG="$3"; START="${4:-1}"  # 4e argument optionnel : étape de reprise
FR="Contraintes : rédige tout le contenu des documents en français (énoncés, titres, glossaire) ; conserve les identifiants de règles et les citations de code. Personne ne peut répondre à tes questions : prends l'option par défaut et note-la dans les questions ouvertes."
TOOLS="Read,Glob,Grep,Write,Edit,Task,Agent,TodoWrite,Bash(ls:*),Bash(find:*),Bash(mkdir:*),Bash(cp:*),Bash(wc:*),Bash(cat:*),Bash(git -C:*),Bash(git rev-parse:*),Bash(git status:*),Bash(git log:*),Bash(python3:*),Bash(python:*),Bash(date:*)"
cd "$KIT"
step() {
  local name="$1" cmd="$2"
  [ "${name%%-*}" -lt "$START" ] && return 0
  echo "=== $name $(date -Is)" >> "$LOG"
  claude -p "$cmd

$FR" --model claude-sonnet-5-5 --add-dir "$APP" --permission-mode acceptEdits \
    --allowedTools "$TOOLS" --output-format json > "$LOG.$name.json" 2>> "$LOG"
  echo "=== $name fin $(date -Is) exit=$?" >> "$LOG"
  # Arrêt si la limite d'usage de l'abonnement est atteinte (la session suivante échouerait aussi)
  if grep -q "hit your session limit" "$LOG.$name.json"; then echo "=== limite atteinte, arrêt" >> "$LOG"; exit 2; fi
}
step 1-init     "/re-init mycomics $APP  (formats de sortie : md et docx ; publication wiki : off)"
step 2-discover "/re-discover"
step 3-domain   "/re-domain"
step 4-ui       "/re-ui front-end"
step 5-verify   "/re-verify 05-user-journeys"
step 6-publish  "/re-publish draft docx"
