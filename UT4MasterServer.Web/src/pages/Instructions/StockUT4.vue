<template>
  <h1>Instructions for Stock UT4</h1>
  <p>
    Use these instructions to use the UT4 Master Server with a standard Windows installation of Unreal Tournament 4
    without UT4UU installed.
  </p>
  <ol>
    <li>Go to <span class="code">{Your Windows User}\Documents\UnrealTournament\Saved\Config\WindowsNoEditor\</span> in
      file explorer</li>
    <li>Open <span class="code">Engine.ini</span> file</li>
    <li>
      Append the following at the bottom of the file:
      <CodeBlock>
        [OnlineSubsystemMcp.BaseServiceMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
        [OnlineSubsystemMcp.GameServiceMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
        [OnlineSubsystemMcp.AccountServiceMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
        [OnlineSubsystemMcp.OnlineFriendsMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
        [OnlineSubsystemMcp.PersonaServiceMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
        [OnlineSubsystemMcp.OnlineImageServiceMcp]<br />
        Domain=master-ut4.timiimit.com<br />
        Protocol=https<br />
      </CodeBlock>
    </li>
    <li>Go to <span class="code">{UT4 Install Location}\Engine\Binaries\Win64\</span></li>
    <li>Create a shortcut to <span class="code">UE4-Win64-Shipping.exe</span> on your desktop (Right click -> Send to ->
      Desktop)</li>
    <li>Open properties of the shortcut (Right click, properties)</li>
    <li>In textbox target at the very end add
      <CodeBlock>
        UnrealTournament -epicapp=UnrealTournamentDev -epicenv=Prod -EpicPortal
      </CodeBlock>
    </li>
    <li>make sure there is space between what was in there before and what you added</li>
    <li>Use the shortcut to start the game</li>
    <li>When prompted to log in, enter your UT4 Master Server username (the one you use to log into this site) instead
      of email</li>
    <li>You may want to configure your settings again as they are stored in the cloud and they will not be transfered over from epic's servers</li>
  </ol>
  <p>To switch back to official epic's servers remove all lines that you added to <span class="code">Engine.ini</span> file. (added lines may have moved)</p>
</template>
<script setup lang="ts">
import CodeBlock from '../../components/CodeBlock.vue';
</script>
