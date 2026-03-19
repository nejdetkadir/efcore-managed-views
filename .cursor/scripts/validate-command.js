const input = JSON.parse(require("fs").readFileSync("/dev/stdin", "utf8"));

const blocked = [
  "rm -rf /",
  "drop database",
  "docker system prune -a",
  "git push --force origin main",
  "git push -f origin main",
  "git reset --hard origin",
];

const isBlocked = blocked.some((cmd) =>
  input.command.toLowerCase().includes(cmd)
);

if (isBlocked) {
  console.log(
    JSON.stringify({
      continue: false,
      permission: "deny",
      userMessage: `Blocked dangerous command: ${input.command}`,
      agentMessage:
        "This command is blocked by the project security policy. Use a safer alternative.",
    })
  );
} else {
  console.log(JSON.stringify({ continue: true, permission: "allow" }));
}
