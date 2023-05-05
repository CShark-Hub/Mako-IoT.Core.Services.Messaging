#  Mako-IoT.Core.Services.Messaging
Message bus with pub-sub and stronly typed data contracts. Uses similar concept as [MassTransit](https://masstransit.io/).

## How to manually sync fork
- Clone repository and navigate into folder
- From command line execute bellow commands
- **git remote add upstream https://github.com/CShark-Hub/Mako-IoT.Base.git**
- **git fetch upstream**
- **git rebase upstream/main**
- If there are any conflicts, resolve them
  - After run **git rebase --continue**
  - Check for conflicts again
- **git push -f origin main**
