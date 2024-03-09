# Table of Contents

- [Controls](#controls 'Controls')
- [Instructions](#instructions 'Instructions')

# Controls

| Action             | Control  |
|--------------------|----------|
| Move               | WASD     |
| Look               | Mouse    |
| Jump               | Space    |
| Interact           | F        |
| Toggle Fullscreen  | `        |
| Open/Close Options | Escape   |

# Instructions

- Requires port forwarding of port 7777 for TCP to work.
- Ensure if prompted that you allow the application through your firewall or this could block your ability to connect.
  - If issues are persisting, add custom firewall rules on your machine to allow port 7777 both in and out.
- If you are the host, you do not need to enter any specific address, however, you will need to share the required information with the various types of clients that are detailed next.
  - Client on the same machine as the host - Enter "localhost" or your IPv4 address.
  - Client on another machine in your local area network - Enter your IPv4 address.
    - Windows - Go to "CMD" and enter "ipconfig" and look for "IPv4 Address".
    - Mac or Linux - Go to the terminal and enter "ifconfig" and look for "inet".
  - Client on a remote network - Share your external IP which can be found by visiting a site such as [IP Chicken](https://ipchicken.com/ "IP Chicken").